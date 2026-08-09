using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class DraftScheduleWindow : Window
{
    private LessonAssignment? _selectedAssignment;
    private LessonAssignment? _draggedAssignment;
    private Point _dragStartPoint;
    private bool _filtersReady;

    public DraftScheduleWindow()
    {
        InitializeComponent();
        InitializeManualEditControls();
        Refresh();
    }

    private void InitializeManualEditControls()
    {
        ManualDayCombo.ItemsSource = new[]
        {
            new DayOption("Pazartesi", DayOfWeek.Monday),
            new DayOption("Salı", DayOfWeek.Tuesday),
            new DayOption("Çarşamba", DayOfWeek.Wednesday),
            new DayOption("Perşembe", DayOfWeek.Thursday),
            new DayOption("Cuma", DayOfWeek.Friday)
        };
        ManualDayCombo.DisplayMemberPath = nameof(DayOption.Name);
        ManualDayCombo.SelectedValuePath = nameof(DayOption.Day);
        ManualHourCombo.ItemsSource = Enumerable.Range(1, 10).ToArray();
    }

    private void Refresh()
    {
        var result = DraftWorkspace.Current;
        SummaryText.Text = result is null
            ? "Henüz taslak üretilmedi."
            : $"Taslak: {result.Assignments.Count} atama · {result.Unassigned.Count} yerleşmeyen talep";
        FillFilters(result);
        BuildDraftMatrix(result);
    }

    private void FillFilters(DraftScheduleResult? result)
    {
        if (_filtersReady || result is null || DraftWorkspace.Requests.Count == 0) return;
        SetItems(ClassFilter, "Sınıf: Tümü", DraftWorkspace.Requests.Select(x => x.Class.Name));
        SetItems(TeacherFilter, "Öğretmen: Tümü", DraftWorkspace.Requests.Select(x => x.Teacher.FullName));
        _filtersReady = true;
    }

    private static void SetItems(ComboBox combo, string allText, IEnumerable<string> values)
    {
        combo.ItemsSource = new[] { allText }.Concat(values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)).ToArray();
        combo.SelectedIndex = 0;
    }

    private void BuildDraftMatrix(DraftScheduleResult? result)
    {
        if (result is null)
        {
            DraftRows.ItemsSource = new List<DraftScheduleRow>();
            return;
        }

        var visibleAssignments = ApplyCurrentFilters(result.Assignments).ToArray();
        var draftRows = new List<DraftScheduleRow>();
        for (var hour = 1; hour <= 10; hour++)
        {
            var row = new DraftScheduleRow { Hour = hour };
            FillDay(row, visibleAssignments, DayOfWeek.Monday, hour, "Monday");
            FillDay(row, visibleAssignments, DayOfWeek.Tuesday, hour, "Tuesday");
            FillDay(row, visibleAssignments, DayOfWeek.Wednesday, hour, "Wednesday");
            FillDay(row, visibleAssignments, DayOfWeek.Thursday, hour, "Thursday");
            FillDay(row, visibleAssignments, DayOfWeek.Friday, hour, "Friday");
            draftRows.Add(row);
        }

        DraftRows.ItemsSource = draftRows;
        if (visibleAssignments.Length != result.Assignments.Count)
            ActionText.Text = $"{visibleAssignments.Length}/{result.Assignments.Count} atama gösteriliyor. Sınıf/öğretmen filtresiyle tek ders hücrelerini sürükleyip taşıyabilirsiniz.";
    }

    private IEnumerable<LessonAssignment> ApplyCurrentFilters(IEnumerable<LessonAssignment> assignments)
    {
        var className = SelectedValue(ClassFilter, "Sınıf: Tümü");
        var teacherName = SelectedValue(TeacherFilter, "Öğretmen: Tümü");
        return assignments.Where(assignment =>
        {
            var request = RequestFor(assignment);
            if (request is null) return false;
            if (className is not null && !string.Equals(request.Class.Name, className, StringComparison.OrdinalIgnoreCase)) return false;
            if (teacherName is not null && !string.Equals(request.Teacher.FullName, teacherName, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        });
    }

    private static void FillDay(DraftScheduleRow row, IReadOnlyList<LessonAssignment> assignmentsSource, DayOfWeek day, int hour, string key)
    {
        var assignments = assignmentsSource.Where(a => a.Day == day && a.LessonNumber == hour).ToList();
        var text = FormatCellText(assignments);
        var color = GetCellColor(assignments);
        switch (key)
        {
            case "Monday": row.MondayText = text; row.MondayColor = color; row.MondayAssignments = assignments; break;
            case "Tuesday": row.TuesdayText = text; row.TuesdayColor = color; row.TuesdayAssignments = assignments; break;
            case "Wednesday": row.WednesdayText = text; row.WednesdayColor = color; row.WednesdayAssignments = assignments; break;
            case "Thursday": row.ThursdayText = text; row.ThursdayColor = color; row.ThursdayAssignments = assignments; break;
            case "Friday": row.FridayText = text; row.FridayColor = color; row.FridayAssignments = assignments; break;
        }
    }

    private static string FormatCellText(List<LessonAssignment> assignments)
    {
        if (assignments.Count == 0) return "";
        if (assignments.Count == 1)
        {
            var req = RequestFor(assignments[0]);
            return $"{req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"}";
        }

        return $"{assignments.Count} ders";
    }

    private static Brush GetCellColor(List<LessonAssignment> assignments)
    {
        if (assignments.Count == 0) return Brushes.White;
        if (assignments.Count > 1) return new SolidColorBrush(Color.FromRgb(219, 234, 254));

        var req = RequestFor(assignments[0]);
        if (!string.IsNullOrWhiteSpace(req?.Teacher.ColorCode))
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(req.Teacher.ColorCode)); }
            catch { return Brushes.LightGray; }
        }

        return Brushes.LightGray;
    }

    private async void Move_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAssignment is null) { ActionText.Text = "Önce tek ders içeren bir hücre seçin."; return; }
        var result = await DraftWorkspace.MoveAsync(_selectedAssignment.Id, _selectedAssignment.LessonNumber + 1);
        ActionText.Text = result.Message;
        if (result.Success) Refresh();
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAssignment is null) { ActionText.Text = "Önce tek ders içeren bir hücre seçin."; return; }
        var result = await DraftWorkspace.RemoveAsync(_selectedAssignment.Id);
        ActionText.Text = result.Message;
        if (result.Success) { _selectedAssignment = null; Refresh(); }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var result = await DraftWorkspace.RestoreLastRemovedAsync();
        ActionText.Text = result.Message;
        if (result.Success) { _selectedAssignment = null; Refresh(); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Excel uyumlu CSV|*.csv", FileName = "ders-taslak.csv" };
        if (dialog.ShowDialog() != true || DraftWorkspace.Current is null) return;
        var lines = new List<string> { "Gün;Ders saati;Sınıf;Ders;Öğretmen;Blok;Manuel" };
        lines.AddRange(DraftWorkspace.Current.Assignments.Select(x =>
        {
            var req = RequestFor(x);
            return $"{GetDayName(x.Day)};{x.LessonNumber};{req?.Class.Name};{req?.Course.Name};{req?.Teacher.FullName};{x.BlockLength};{x.IsManual}";
        }));
        File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
        ActionText.Text = "CSV dışa aktarıldı; Excel ile açılabilir.";
    }

    private void DraftCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not DraftScheduleRow row || border.Tag is not string dayTag) return;
        var assignments = AssignmentsFor(row, dayTag);

        if (assignments.Count == 1)
        {
            _selectedAssignment = assignments[0];
            _dragStartPoint = e.GetPosition(null);
            ShowAssignmentDetails(_selectedAssignment);
        }
        else if (assignments.Count > 1)
        {
            _selectedAssignment = null;
            ActionText.Text = $"{assignments.Count} ders aynı okul saatinde görünüyor. Bu tek başına çakışma değildir. Sürükle-bırak için sınıf veya öğretmen filtresi seçin.";
            SelectedAssignmentText.Text = $"{assignments.Count} okul geneli ders var.";
            DetailPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            _selectedAssignment = null;
            SelectedAssignmentText.Text = "Henüz bir atama seçilmedi.";
            DetailPanel.Visibility = Visibility.Collapsed;
        }

        e.Handled = true;
    }

    private void ShowAssignmentDetails(LessonAssignment assignment)
    {
        var req = RequestFor(assignment);
        ActionText.Text = $"Seçili: {req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"} ({req?.Teacher.FullName ?? "?"}) | {GetDayName(assignment.Day)} {assignment.LessonNumber}. saat";
        SelectedAssignmentText.Text = $"{req?.Class.Name ?? "?"} - {req?.Course.Name ?? "?"}";
        DetailClass.Text = req?.Class.Name ?? "?";
        DetailCourse.Text = req?.Course.Name ?? "?";
        DetailTeacher.Text = req?.Teacher.FullName ?? "?";
        DetailResource.Text = req?.Resource?.Name ?? "-";
        DetailTime.Text = $"{GetDayName(assignment.Day)} - {assignment.LessonNumber}. saat";
        DetailStatus.Text = assignment.IsManual ? "Manuel korundu" : "Otomatik taslak";
        ManualDayCombo.SelectedValue = assignment.Day;
        ManualHourCombo.SelectedItem = assignment.LessonNumber;
        DetailPanel.Visibility = Visibility.Visible;
    }

    private async void ManualMove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAssignment is null)
        {
            ActionText.Text = "Önce tek ders içeren bir hücre seçin.";
            return;
        }

        if (ManualDayCombo.SelectedValue is not DayOfWeek targetDay || ManualHourCombo.SelectedItem is not int targetHour)
        {
            ActionText.Text = "Taşıma için gün ve ders saati seçin.";
            return;
        }

        var result = await DraftWorkspace.MoveAsync(_selectedAssignment.Id, targetHour, targetDay);
        ActionText.Text = result.Message;
        if (result.Success)
        {
            _selectedAssignment = null;
            Refresh();
        }
    }

    private static LessonRequest? RequestFor(LessonAssignment assignment) =>
        DraftWorkspace.Requests.FirstOrDefault(r => r.Class.Id == assignment.ClassId && r.Course.Id == assignment.CourseId && r.Teacher.Id == assignment.TeacherId);

    private static List<LessonAssignment> AssignmentsFor(DraftScheduleRow row, string dayTag) => dayTag switch
    {
        "Monday" => row.MondayAssignments,
        "Tuesday" => row.TuesdayAssignments,
        "Wednesday" => row.WednesdayAssignments,
        "Thursday" => row.ThursdayAssignments,
        "Friday" => row.FridayAssignments,
        _ => new List<LessonAssignment>()
    };

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_filtersReady) return;
        _selectedAssignment = null;
        DetailPanel.Visibility = Visibility.Collapsed;
        Refresh();
    }

    private void DraftCell_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is not Border border || border.DataContext is not DraftScheduleRow row || border.Tag is not string dayTag) return;
        var assignments = AssignmentsFor(row, dayTag);
        if (assignments.Count != 1) return;

        _draggedAssignment = assignments[0];
        var data = new DataObject(DataFormats.Text, _draggedAssignment.Id.ToString());
        DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
    }

    private void DraftCell_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.BorderBrush = Brushes.DodgerBlue; border.BorderThickness = new Thickness(2); }
    }

    private void DraftCell_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.ClearValue(Border.BorderBrushProperty); border.ClearValue(Border.BorderThicknessProperty); }
    }

    private async void DraftCell_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border droppedOn) { droppedOn.ClearValue(Border.BorderBrushProperty); droppedOn.ClearValue(Border.BorderThicknessProperty); }
        if (_draggedAssignment is null || sender is not Border targetBorder || targetBorder.DataContext is not DraftScheduleRow targetRow || targetBorder.Tag is not string targetDayTag) return;

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

        var result = await DraftWorkspace.MoveAsync(_draggedAssignment.Id, targetRow.Hour, targetDay);
        ActionText.Text = result.Message;
        if (result.Success) Refresh();
        _draggedAssignment = null;
    }

    private static string? SelectedValue(ComboBox combo, string allText) => combo.SelectedItem is string value && value != allText ? value : null;

    private static string GetDayName(DayOfWeek day) => day switch
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

    private sealed record DayOption(string Name, DayOfWeek Day);
}
