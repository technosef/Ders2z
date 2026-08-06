using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Printing;
using System.Windows.Xps;
using System.IO;
using DersDagitim.Application;

namespace DersDagitim.Wpf;
public partial class AscScheduleWindow : Window
{
    private IReadOnlyList<AscScheduleCard> _cards = Array.Empty<AscScheduleCard>();
    private AscScheduleCard? _selectedCard;
    private AscScheduleCard? _draggedCard;
    private int _dragFromHour = -1;
    private string _dragFromDay = "";
    private Point _dragStartPoint;
    public AscScheduleWindow() { InitializeComponent(); Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync() { _cards = await App.Dashboard.Repository.GetAscScheduleCardsAsync(); ApplyFilter(); StatusText.Text = $"{_cards.Count} mevcut ASC kartı · kaldırılanlar korunur"; }
    private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        var text = FilterText?.Text?.Trim() ?? "";
        var day = (DayCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString();
        
        // Filtrelenmiş kartlar
        var filteredCards = _cards.Where(x => 
            (string.IsNullOrWhiteSpace(text) || $"{x.ClassName} {x.CourseName} {x.TeacherName} {x.ResourceName}".Contains(text, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(day) || day == "Tüm günler" || x.DayName == day)).ToList();
        
        // Matris yapısını oluştur (10 saat x 5 gün)
        var scheduleRows = new List<ScheduleRow>();
        for (int hour = 1; hour <= 10; hour++)
        {
            var row = new ScheduleRow { Hour = hour };
            
            // Her gün için hücreyi doldur
            var mondayCard = filteredCards.FirstOrDefault(c => c.DayName == "Pazartesi" && c.Period == hour);
            row.MondayText = mondayCard != null ? $"{mondayCard.ClassName} - {mondayCard.CourseName}" : "";
            row.MondayColor = mondayCard != null ? BrushFor(mondayCard.TeacherColor) : Brushes.White;
            row.MondayCard = mondayCard;
            
            var tuesdayCard = filteredCards.FirstOrDefault(c => c.DayName == "Salı" && c.Period == hour);
            row.TuesdayText = tuesdayCard != null ? $"{tuesdayCard.ClassName} - {tuesdayCard.CourseName}" : "";
            row.TuesdayColor = tuesdayCard != null ? BrushFor(tuesdayCard.TeacherColor) : Brushes.White;
            row.TuesdayCard = tuesdayCard;
            
            var wednesdayCard = filteredCards.FirstOrDefault(c => c.DayName == "Çarşamba" && c.Period == hour);
            row.WednesdayText = wednesdayCard != null ? $"{wednesdayCard.ClassName} - {wednesdayCard.CourseName}" : "";
            row.WednesdayColor = wednesdayCard != null ? BrushFor(wednesdayCard.TeacherColor) : Brushes.White;
            row.WednesdayCard = wednesdayCard;
            
            var thursdayCard = filteredCards.FirstOrDefault(c => c.DayName == "Perşembe" && c.Period == hour);
            row.ThursdayText = thursdayCard != null ? $"{thursdayCard.ClassName} - {thursdayCard.CourseName}" : "";
            row.ThursdayColor = thursdayCard != null ? BrushFor(thursdayCard.TeacherColor) : Brushes.White;
            row.ThursdayCard = thursdayCard;
            
            var fridayCard = filteredCards.FirstOrDefault(c => c.DayName == "Cuma" && c.Period == hour);
            row.FridayText = fridayCard != null ? $"{fridayCard.ClassName} - {fridayCard.CourseName}" : "";
            row.FridayColor = fridayCard != null ? BrushFor(fridayCard.TeacherColor) : Brushes.White;
            row.FridayCard = fridayCard;
            
            scheduleRows.Add(row);
        }
        
        ScheduleRows.ItemsSource = scheduleRows;
    }
    private async void Move_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) { StatusText.Text = "Önce matristen bir kart seçin."; return; }
        if (!int.TryParse(PeriodText.Text, out var period) || period < 1 || period > 10) { StatusText.Text = "Hedef saat 1-10 arasında olmalı."; return; }
        var day = (DayCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(); 
        if (string.IsNullOrWhiteSpace(day) || day == "Tüm günler") day = _selectedCard.DayName;
        var conflict = _cards.Where(x => x.CardId != _selectedCard.CardId && !x.IsRemoved && x.DayName == day && 
            Overlaps(x.Period, x.BlockLength, period, _selectedCard.BlockLength) && 
            (x.ClassName == _selectedCard.ClassName || x.TeacherName == _selectedCard.TeacherName || 
             (!string.IsNullOrWhiteSpace(_selectedCard.ResourceName) && x.ResourceName == _selectedCard.ResourceName))).FirstOrDefault();
        if (conflict is not null) { 
            StatusText.Text = $"Taşıma reddedildi: {conflict.ClassName} / {conflict.CourseName} ile sınıf, öğretmen veya kaynak çakışması."; 
            return; 
        }
        await App.Dashboard.Repository.SaveAscCardOverrideAsync(new AscCardOverride(_selectedCard.CardId, period, DayBits(day), false)); 
        await LoadAsync(); 
        StatusText.Text = "Kart taşındı ve SQLite'a manuel değişiklik olarak kaydedildi.";
    }
    private async void Remove_Click(object sender, RoutedEventArgs e) 
    {
        if (_selectedCard == null) { StatusText.Text = "Önce matristen bir kart seçin."; return; }
        await App.Dashboard.Repository.SaveAscCardOverrideAsync(new AscCardOverride(_selectedCard.CardId, _selectedCard.Period, DayBits(_selectedCard.DayName), true)); 
        await LoadAsync(); 
        StatusText.Text = "Kart kaldırıldı; ASC kaydı silinmedi, manuel kaldırma olarak işaretlendi."; 
        _selectedCard = null;
    }
    private async void Restore_Click(object sender, RoutedEventArgs e) 
    {
        if (_selectedCard == null) { StatusText.Text = "Önce matristen bir kart seçin."; return; }
        await App.Dashboard.Repository.SaveAscCardOverrideAsync(new AscCardOverride(_selectedCard.CardId, _selectedCard.Period, DayBits(_selectedCard.DayName), false)); 
        await LoadAsync(); 
        StatusText.Text = "Kart yeniden görünür yapıldı.";
        _selectedCard = null;
    }
    
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;
            
            ScheduleGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            ScheduleGrid.Arrange(new Rect(0, 0, ScheduleGrid.DesiredSize.Width, ScheduleGrid.DesiredSize.Height));
            ScheduleGrid.UpdateLayout();
            
            printDialog.PrintVisual(ScheduleGrid, "Ders Programı - aSc Formatında");
            StatusText.Text = "PDF/yazıcıya gönderildi. PDF printer seçerek dosya kaydedebilirsiniz.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"PDF/yazdırma hatası: {ex.Message}";
        }
    }
    
    private static bool Overlaps(int a, int al, int b, int bl) => a < b + bl && b < a + al;
    private static string DayBits(string day) => day switch { "Pazartesi" => "10000", "Salı" => "01000", "Çarşamba" => "00100", "Perşembe" => "00010", "Cuma" => "00001", _ => "10000" };
    
    // Hücre tıklama event handler
    private void Cell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is ScheduleRow row && border.Tag is string dayTag)
        {
            AscScheduleCard? card = dayTag switch
            {
                "Monday" => row.MondayCard,
                "Tuesday" => row.TuesdayCard,
                "Wednesday" => row.WednesdayCard,
                "Thursday" => row.ThursdayCard,
                "Friday" => row.FridayCard,
                _ => null
            };
            
            if (card != null && !card.IsRemoved)
            {
                _selectedCard = card;
                _dragStartPoint = e.GetPosition(null);
                StatusText.Text = $"Seçili: {card.ClassName} - {card.CourseName} ({card.TeacherName}) | {card.DayName} {card.Period}. saat";
                
                // Detay panelini doldur
                SelectedCardText.Text = $"{card.ClassName} - {card.CourseName}";
                DetailClass.Text = card.ClassName;
                DetailCourse.Text = card.CourseName;
                DetailTeacher.Text = card.TeacherName;
                DetailResource.Text = string.IsNullOrWhiteSpace(card.ResourceName) ? "-" : card.ResourceName;
                DetailTime.Text = $"{card.DayName} - {card.Period}. saat";
                DetailStatus.Text = card.IsRemoved ? "Manuel kaldırıldı" : card.IsManualOverride ? "Manuel değiştirildi" : "Mevcut ASC";
                DetailPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _selectedCard = null;
                SelectedCardText.Text = "Henüz bir kart seçilmedi.";
                DetailPanel.Visibility = Visibility.Collapsed;
            }
            e.Handled = true;
        }
    }
    
    // Drag-drop baslangic
    private void Cell_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is Border border && border.DataContext is ScheduleRow row && border.Tag is string dayTag)
        {
            var card = dayTag switch
            {
                "Monday" => row.MondayCard,
                "Tuesday" => row.TuesdayCard,
                "Wednesday" => row.WednesdayCard,
                "Thursday" => row.ThursdayCard,
                "Friday" => row.FridayCard,
                _ => null
            };
            
            if (card != null && !card.IsRemoved)
            {
                _draggedCard = card;
                _dragFromHour = row.Hour;
                _dragFromDay = dayTag;
                var data = new DataObject(DataFormats.Text, card.CardId.ToString());
                DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
            }
        }
    }
    
    // Sürüklenen kart hedef hücrenin üzerine geldiğinde görsel geri bildirim
    private void Cell_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.BorderBrush = Brushes.DodgerBlue; border.BorderThickness = new Thickness(2); }
    }
    private void Cell_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border) { border.ClearValue(Border.BorderBrushProperty); border.ClearValue(Border.BorderThicknessProperty); }
    }

    // Drag-drop bitis
    private async void Cell_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border droppedOn) { droppedOn.ClearValue(Border.BorderBrushProperty); droppedOn.ClearValue(Border.BorderThicknessProperty); }
        if (_draggedCard == null || !(sender is Border targetBorder && targetBorder.DataContext is ScheduleRow targetRow && targetBorder.Tag is string targetDayTag)) return;
        
        var targetDay = targetDayTag switch
        {
            "Monday" => "Pazartesi",
            "Tuesday" => "Salı",
            "Wednesday" => "Çarşamba",
            "Thursday" => "Perşembe",
            "Friday" => "Cuma",
            _ => ""
        };
        
        if (string.IsNullOrEmpty(targetDay)) return;
        
        var targetHour = targetRow.Hour;
        var targetPeriod = targetHour;
        
        // Çakışma kontrolü
        var conflict = _cards.Where(x => x.CardId != _draggedCard.CardId && !x.IsRemoved && x.DayName == targetDay && 
            Overlaps(x.Period, x.BlockLength, targetPeriod, _draggedCard.BlockLength) && 
            (x.ClassName == _draggedCard.ClassName || x.TeacherName == _draggedCard.TeacherName || 
             (!string.IsNullOrWhiteSpace(_draggedCard.ResourceName) && x.ResourceName == _draggedCard.ResourceName))).FirstOrDefault();
        
        if (conflict is not null)
        {
            StatusText.Text = $"Taşıma reddedildi: {conflict.ClassName} / {conflict.CourseName} ile çakışma";
            _draggedCard = null;
            return;
        }
        
        // Taşıma işlemi
        await App.Dashboard.Repository.SaveAscCardOverrideAsync(new AscCardOverride(_draggedCard.CardId, targetPeriod, DayBits(targetDay), false));
        await LoadAsync();
        StatusText.Text = $"Kart {targetDay} {targetPeriod}. saate taşındı";
        _draggedCard = null;
    }
    
    // Matris satırı için view model
    private sealed class ScheduleRow
    {
        public int Hour { get; set; }
        public string MondayText { get; set; } = "";
        public Brush MondayColor { get; set; } = Brushes.White;
        public AscScheduleCard? MondayCard { get; set; }
        
        public string TuesdayText { get; set; } = "";
        public Brush TuesdayColor { get; set; } = Brushes.White;
        public AscScheduleCard? TuesdayCard { get; set; }
        
        public string WednesdayText { get; set; } = "";
        public Brush WednesdayColor { get; set; } = Brushes.White;
        public AscScheduleCard? WednesdayCard { get; set; }
        
        public string ThursdayText { get; set; } = "";
        public Brush ThursdayColor { get; set; } = Brushes.White;
        public AscScheduleCard? ThursdayCard { get; set; }
        
        public string FridayText { get; set; } = "";
        public Brush FridayColor { get; set; } = Brushes.White;
        public AscScheduleCard? FridayCard { get; set; }
    }
    
    private sealed class Row(AscScheduleCard source)
    {
        public long CardId => source.CardId; public string ClassName => source.ClassName; public string CourseName => source.CourseName; public string TeacherName => source.TeacherName; public string ResourceName => source.ResourceName; public string DayName => source.DayName; public int Period => source.Period; public int BlockLength => source.BlockLength; public Brush ColorBrush => BrushFor(source.TeacherColor); public string Status => source.IsRemoved ? "Manuel kaldırıldı" : source.IsManualOverride ? "Manuel değiştirildi" : "Mevcut ASC";
        private static Brush BrushFor(string value) { try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(value) ? "#E2E8F0" : value)); } catch { return Brushes.LightGray; } }
    }
    
    private static Brush BrushFor(string value)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(value) ? "#E2E8F0" : value)); }
        catch { return Brushes.LightGray; }
    }
}
