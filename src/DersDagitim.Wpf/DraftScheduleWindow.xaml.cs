using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;
public partial class DraftScheduleWindow : Window
{
    private LessonAssignment? _selectedAssignment;
    private LessonAssignment? _draggedAssignment;
    private Point _dragStartPoint;
    
    public DraftScheduleWindow() { InitializeComponent(); Refresh(); }
    
    private void Refresh()
    {
        var result = DraftWorkspace.Current;
        SummaryText.Text = result is null ? "Henüz taslak üretilmedi." : $"Taslak: {result.Assignments.Count} atama · {result.Unassigned.Count} yerleşmeyen talep";
        BuildDraftMatrix(result);
    }
    
    private void BuildDraftMatrix(DraftScheduleResult? result)
    {
        if (result == null)
        {
            DraftRows.ItemsSource = new List<DraftScheduleRow>();
            return;
        }
        
        var draftRows = new List<DraftScheduleRow>();
        for (int hour = 1; hour <= 10; hour++)
        {
            var row = new DraftScheduleRow { Hour = hour };
            
            var mondayAssignments = result.Assignments.Where(a => a.Day == DayOfWeek.Monday && a.LessonNumber == hour).ToList();
            row.MondayText = FormatCellText(mondayAssignments);
            row.MondayColor = GetCellColor(mondayAssignments);
            row.MondayAssignments = mondayAssignments;
            
            var tuesdayAssignments = result.Assignments.Where(a => a.Day == DayOfWeek.Tuesday && a.LessonNumber == hour).ToList();
            row.TuesdayText = FormatCellText(tuesdayAssignments);
            row.TuesdayColor = GetCellColor(tuesdayAssignments);
            row.TuesdayAssignments = tuesdayAssignments;
            
            var wednesdayAssignments = result.Assignments.Where(a => a.Day == DayOfWeek.Wednesday && a.LessonNumber == hour).ToList();
            row.WednesdayText = FormatCellText(wednesdayAssignments);
            row.WednesdayColor = GetCellColor(wednesdayAssignments);
            row.WednesdayAssignments = wednesdayAssignments;
            
            var thursdayAssignments = result.Assignments.Where(a => a.Day == DayOfWeek.Thursday && a.LessonNumber == hour).ToList();
            row.ThursdayText = FormatCellText(thursdayAssignments);
            row.ThursdayColor = GetCellColor(thursdayAssignments);
            row.ThursdayAssignments = thursdayAssignments;
            
            var fridayAssignments = result.Assignments.Where(a => a.Day == DayOfWeek.Friday && a.LessonNumber == hour).ToList();
            row.FridayText = FormatCellText(fridayAssignments);
            row.FridayColor = GetCellColor(fridayAssignments);
            row.FridayAssignments = fridayAssignments;
            
            draftRows.Add(row);
        }
        
        DraftRows.ItemsSource = draftRows;
    }
    
    private string FormatCellText(List<LessonAssignment> assignments)
    {
        if (assignments.Count == 0) return "";
        if (assignments.Count == 1)
        {
            var req = DraftWorkspace.Requests.FirstOrDefault(r => r.Class.Id == assignments[0].ClassId && r.Course.Id == assignments[0].CourseId);
            return $"{req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"}";
        }
        return $"{assignments.Count} atama";
    }
    
    private Brush GetCellColor(List<LessonAssignment> assignments)
    {
        if (assignments.Count == 0) return Brushes.White;
        if (assignments.Count > 1) return Brushes.LightSalmon;
        
        var assignment = assignments[0];
        var req = DraftWorkspace.Requests.FirstOrDefault(r => r.Class.Id == assignment.ClassId && r.Course.Id == assignment.CourseId);
        if (req?.Teacher.ColorCode != null)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(req.Teacher.ColorCode)); }
            catch { return Brushes.LightGray; }
        }
        return Brushes.LightGray;
    }
    
    private void Move_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAssignment == null) { ActionText.Text = "Önce matristen bir atama seçin."; return; }
        var result = DraftWorkspace.Move(_selectedAssignment.Id, _selectedAssignment.LessonNumber + 1);
        ActionText.Text = result.Message;
        if (result.Success) Refresh();
    }
    
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAssignment == null) { ActionText.Text = "Önce matristen bir atama seçin."; return; }
        DraftWorkspace.Remove(_selectedAssignment.Id);
        ActionText.Text = "Atama kaldırıldı; otomatik taslağın dışında tutulur.";
        _selectedAssignment = null;
        Refresh();
    }
    
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Excel uyumlu CSV|*.csv", FileName = "ders-taslak.csv" };
        if (dialog.ShowDialog() != true || DraftWorkspace.Current is null) return;
        var lines = new List<string> { "Gün;Ders saati;Sınıf;Ders;Blok;Manuel" };
        lines.AddRange(DraftWorkspace.Current.Assignments.Select(x => $"{x.Day};{x.LessonNumber};{x.ClassId};{x.CourseId};{x.BlockLength};{x.IsManual}"));
        File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
        ActionText.Text = "CSV dışa aktarıldı; Excel ile açılabilir.";
    }
    
    private void DraftCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is DraftScheduleRow row && border.Tag is string dayTag)
        {
            var assignments = dayTag switch
            {
                "Monday" => row.MondayAssignments,
                "Tuesday" => row.TuesdayAssignments,
                "Wednesday" => row.WednesdayAssignments,
                "Thursday" => row.ThursdayAssignments,
                "Friday" => row.FridayAssignments,
                _ => new List<LessonAssignment>()
            };
            
            if (assignments.Count == 1)
            {
                _selectedAssignment = assignments[0];
                _dragStartPoint = e.GetPosition(null);
                var req = DraftWorkspace.Requests.FirstOrDefault(r => r.Class.Id == _selectedAssignment.ClassId && r.Course.Id == _selectedAssignment.CourseId);
                ActionText.Text = $"Seçili: {req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"} ({req?.Teacher.FullName ?? "?"}) | {_selectedAssignment.Day} {_selectedAssignment.LessonNumber}. saat";
                
                // Detay panelini doldur
                SelectedAssignmentText.Text = $"{req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"}";
                DetailClass.Text = req?.Class.Name ?? "?";
                DetailCourse.Text = req?.Course.Name ?? "?";
                DetailTeacher.Text = req?.Teacher.FullName ?? "?";
                DetailResource.Text = req?.Resource?.Name ?? "-";
                DetailTime.Text = $"{GetDayName(_selectedAssignment.Day)} - {_selectedAssignment.LessonNumber}. saat";
                DetailStatus.Text = _selectedAssignment.IsManual ? "Manuel korundu" : "Otomatik taslak";
                DetailPanel.Visibility = Visibility.Visible;
            }
            else if (assignments.Count > 1)
            {
                ActionText.Text = $"{assignments.Count} atama çakışıyor";
                _selectedAssignment = assignments[0];
            }
            else
            {
                _selectedAssignment = null;
                SelectedAssignmentText.Text = "Henüz bir atama seçilmedi.";
                DetailPanel.Visibility = Visibility.Collapsed;
            }
            e.Handled = true;
        }
    }
    
    // Sürükleme başlangıcı: eşik aşılmadan sürükleme tetiklenmesin
    private void DraftCell_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is not Border border || border.DataContext is not DraftScheduleRow row || border.Tag is not string dayTag) return;
        var assignments = dayTag switch
        {
            "Monday" => row.MondayAssignments,
            "Tuesday" => row.TuesdayAssignments,
            "Wednesday" => row.WednesdayAssignments,
            "Thursday" => row.ThursdayAssignments,
            "Friday" => row.FridayAssignments,
            _ => new List<LessonAssignment>()
        };
        if (assignments.Count != 1) return; // boş veya çakışan (birden fazla) hücre sürüklenemez

        _draggedAssignment = assignments[0];
        var data = new DataObject(DataFormats.Text, _draggedAssignment.Id.ToString());
        DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
    }

    // Sürüklenen atama hedef hücrenin üzerine geldiğinde görsel geri bildirim
    private void DraftCell_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.BorderBrush = Brushes.DodgerBlue; border.BorderThickness = new Thickness(2); }
    }
    private void DraftCell_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.ClearValue(Border.BorderBrushProperty); border.ClearValue(Border.BorderThicknessProperty); }
    }

    // Sürükleme bitişi: hedef gün/saate taşı
    private void DraftCell_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border droppedOn) { droppedOn.ClearValue(Border.BorderBrushProperty); droppedOn.ClearValue(Border.BorderThicknessProperty); }
        if (_draggedAssignment == null || sender is not Border targetBorder || targetBorder.DataContext is not DraftScheduleRow targetRow || targetBorder.Tag is not string targetDayTag) return;

        DayOfWeek? targetDay = targetDayTag switch
        {
            "Monday" => DayOfWeek.Monday,
            "Tuesday" => DayOfWeek.Tuesday,
            "Wednesday" => DayOfWeek.Wednesday,
            "Thursday" => DayOfWeek.Thursday,
            "Friday" => DayOfWeek.Friday,
            _ => null
        };
        if (targetDay is null) { _draggedAssignment = null; return; }

        var result = DraftWorkspace.Move(_draggedAssignment.Id, targetRow.Hour, targetDay);
        ActionText.Text = result.Message;
        if (result.Success) Refresh();
        _draggedAssignment = null;
    }

    private string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        _ => "-"
    };
    
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;
            
            DraftGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            DraftGrid.Arrange(new Rect(0, 0, DraftGrid.DesiredSize.Width, DraftGrid.DesiredSize.Height));
            DraftGrid.UpdateLayout();
            
            printDialog.PrintVisual(DraftGrid, "Taslak Ders Programı");
            ActionText.Text = "PDF/yazıcıya gönderildi.";
        }
        catch (Exception ex)
        {
            ActionText.Text = $"PDF hatası: {ex.Message}";
        }
    }
    
    private sealed class DraftScheduleRow
    {
        public int Hour { get; set; }
        public string MondayText { get; set; } = "";
        public Brush MondayColor { get; set; } = Brushes.White;
        public List<LessonAssignment> MondayAssignments { get; set; } = new();
        
        public string TuesdayText { get; set; } = "";
        public Brush TuesdayColor { get; set; } = Brushes.White;
        public List<LessonAssignment> TuesdayAssignments { get; set; } = new();
        
        public string WednesdayText { get; set; } = "";
        public Brush WednesdayColor { get; set; } = Brushes.White;
        public List<LessonAssignment> WednesdayAssignments { get; set; } = new();
        
        public string ThursdayText { get; set; } = "";
        public Brush ThursdayColor { get; set; } = Brushes.White;
        public List<LessonAssignment> ThursdayAssignments { get; set; } = new();
        
        public string FridayText { get; set; } = "";
        public Brush FridayColor { get; set; } = Brushes.White;
        public List<LessonAssignment> FridayAssignments { get; set; } = new();
    }
}
