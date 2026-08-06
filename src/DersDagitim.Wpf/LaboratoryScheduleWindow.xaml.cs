using System.Windows; using DersDagitim.Domain;
namespace DersDagitim.Wpf;
public partial class LaboratoryScheduleWindow : Window
{
    public LaboratoryScheduleWindow() { InitializeComponent(); ScheduleGrid.ItemsSource = LaboratoryScheduleSample.Cells.Select(x => new Row(x)); }
    private sealed class Row(LaboratoryScheduleCell cell) { public string DayName => cell.DayName; public string ClassName => string.IsNullOrWhiteSpace(cell.ClassName) ? "—" : cell.ClassName; public string LessonRange => cell.LessonRange; public string TimeRange => $"{Time(cell.StartLesson).Start}–{Time(cell.EndLesson).End}"; public string DisplayText => cell.DisplayText; public string BlockDescription => cell.IsAvailable ? "Atölye uygun" : $"{cell.LessonSpan} ardışık saat"; private static LessonTime Time(int n) => LaboratoryScheduleSample.Times[n - 1]; }
}
