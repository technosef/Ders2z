using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class LaboratoryScheduleWindow : Window
{
    private IReadOnlyList<LessonAssignment> _assignments = Array.Empty<LessonAssignment>();
    private IReadOnlyList<LessonRequest> _requests = Array.Empty<LessonRequest>();
    private IReadOnlyList<Teacher> _teachers = Array.Empty<Teacher>();
    private bool _ready;

    public LaboratoryScheduleWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (DraftWorkspace.Current is not null && DraftWorkspace.Requests.Count > 0)
        {
            _assignments = DraftWorkspace.Current.Assignments;
            _requests = DraftWorkspace.Requests;
            _teachers = DraftWorkspace.Teachers;
            StatusText.Text = "Taslak çizelge verisi kullanılıyor.";
        }
        else
        {
            var input = await App.Dashboard.Repository.GetAscSolverInputAsync(true);
            _assignments = input.ProtectedCards;
            _requests = input.Requests;
            _teachers = await App.Dashboard.Repository.GetTeachersAsync();
            StatusText.Text = "Taslak yok; mevcut ASC kartları kullanılıyor.";
        }

        ModePicker.ItemsSource = new[] { "Sınıf", "Kaynak/Lab" };
        ModePicker.SelectedIndex = 0;
        _ready = true;
        FillEntities();
        ApplyFilter();
    }

    private void FillEntities()
    {
        if (ModePicker.SelectedItem?.ToString() == "Kaynak/Lab")
        {
            EntityPicker.ItemsSource = new[] { "Tüm kaynaklar" }
                .Concat(_requests.Select(x => x.Resource?.Name).Where(x => !string.IsNullOrWhiteSpace(x))!.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                .ToArray();
        }
        else
        {
            EntityPicker.ItemsSource = new[] { "Tüm sınıflar" }
                .Concat(_requests.Select(x => x.Class.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                .ToArray();
        }

        EntityPicker.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        if (!_ready) return;

        var selected = EntityPicker.SelectedItem?.ToString();
        var mode = ModePicker.SelectedItem?.ToString();
        var rows = _assignments
            .Select(x => (Assignment: x, Request: RequestFor(x)))
            .Where(x => x.Request is not null)
            .Where(x =>
                mode == "Kaynak/Lab"
                    ? selected == "Tüm kaynaklar" || string.Equals(x.Request!.Resource?.Name, selected, StringComparison.OrdinalIgnoreCase)
                    : selected == "Tüm sınıflar" || string.Equals(x.Request!.Class.Name, selected, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => DayOrder(x.Assignment.Day))
            .ThenBy(x => x.Assignment.LessonNumber)
            .ThenBy(x => x.Request!.Class.Name)
            .Select(x => new Row(
                DayName(x.Assignment.Day),
                x.Assignment.LessonNumber,
                x.Request!.Class.Name,
                x.Request.Course.Name,
                TeacherNameFor(x.Assignment),
                x.Request.Resource?.Name ?? "-",
                x.Assignment.IsManual ? "Manuel" : "ASC/Taslak"))
            .ToArray();

        ScheduleGrid.ItemsSource = rows;
        StatusText.Text = $"{rows.Length} ders satırı gösteriliyor. Görünüm: {mode} · Seçim: {selected}";
    }

    private LessonRequest? RequestFor(LessonAssignment assignment) =>
        _requests.FirstOrDefault(x => x.Class.Id == assignment.ClassId && x.Course.Id == assignment.CourseId && x.Teacher.Id == assignment.TeacherId)
        ?? _requests.FirstOrDefault(x => x.Class.Id == assignment.ClassId && x.Course.Id == assignment.CourseId);

    private string TeacherNameFor(LessonAssignment assignment) =>
        _teachers.FirstOrDefault(x => x.Id == assignment.TeacherId)?.FullName
        ?? RequestFor(assignment)?.Teacher.FullName
        ?? "?";

    private void ModePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        FillEntities();
        ApplyFilter();
    }

    private void EntityPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var rows = (ScheduleGrid.ItemsSource as IEnumerable<Row>)?.ToArray() ?? Array.Empty<Row>();
        if (rows.Length == 0) { StatusText.Text = "Aktarılacak satır yok."; return; }

        var dialog = new SaveFileDialog { Filter = "Excel uyumlu CSV|*.csv", FileName = SafeFileName($"{ModePicker.SelectedItem}-{EntityPicker.SelectedItem}-program.csv") };
        if (dialog.ShowDialog() != true) return;

        var lines = new List<string> { "Gün;Ders saati;Sınıf;Ders;Öğretmen;Kaynak;Durum" };
        lines.AddRange(rows.Select(x => $"{x.DayName};{x.LessonNumber};{x.ClassName};{x.CourseName};{x.TeacherName};{x.ResourceName};{x.Status}"));
        File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
        StatusText.Text = "CSV dışa aktarıldı.";
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
            printDialog.PrintVisual(ScheduleGrid, $"{ModePicker.SelectedItem} haftalık program");
            StatusText.Text = "PDF/yazıcıya gönderildi. PDF printer seçerek dosya kaydedebilirsiniz.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"PDF/yazdırma hatası: {ex.Message}";
        }
    }

    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '-');
        return value.Replace(' ', '-').ToLowerInvariant();
    }

    private static int DayOrder(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => 1,
        DayOfWeek.Tuesday => 2,
        DayOfWeek.Wednesday => 3,
        DayOfWeek.Thursday => 4,
        DayOfWeek.Friday => 5,
        _ => 9
    };

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        _ => day.ToString()
    };

    private sealed record Row(string DayName, int LessonNumber, string ClassName, string CourseName, string TeacherName, string ResourceName, string Status);
}
