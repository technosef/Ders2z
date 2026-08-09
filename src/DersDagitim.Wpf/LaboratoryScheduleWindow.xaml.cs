using System.IO;
using System.Net;
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
    private IReadOnlyList<SchoolClass> _classes = Array.Empty<SchoolClass>();
    private IReadOnlyList<ClassTeacherAssignment> _classTeachers = Array.Empty<ClassTeacherAssignment>();
    private bool _ready;

    public LaboratoryScheduleWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _classes = await App.Dashboard.Repository.GetClassesAsync();
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();

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

        ClassTeacherCombo.ItemsSource = _teachers.OrderBy(x => x.FullName).Select(x => new TeacherItem(x.Id, x.FullName)).ToArray();
        ClassTeacherCombo.DisplayMemberPath = nameof(TeacherItem.Name);
        ClassTeacherCombo.SelectedValuePath = nameof(TeacherItem.Id);

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
        SyncClassTeacherPanel();
    }

    private void ApplyFilter()
    {
        if (!_ready) return;

        var selected = EntityPicker.SelectedItem?.ToString();
        var mode = ModePicker.SelectedItem?.ToString();
        SyncClassTeacherPanel();

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

    private void SyncClassTeacherPanel()
    {
        var selected = EntityPicker.SelectedItem?.ToString();
        var specificClass = ModePicker.SelectedItem?.ToString() == "Sınıf" && !string.IsNullOrWhiteSpace(selected) && selected != "Tüm sınıflar";
        ClassTeacherPanel.Visibility = specificClass ? Visibility.Visible : Visibility.Collapsed;
        if (!specificClass)
        {
            ClassTeacherCombo.SelectedIndex = -1;
            return;
        }

        var schoolClass = _classes.FirstOrDefault(x => string.Equals(x.Name, selected, StringComparison.OrdinalIgnoreCase));
        var assigned = schoolClass is null ? null : _classTeachers.FirstOrDefault(x => x.ClassId == schoolClass.Id);
        ClassTeacherCombo.SelectedValue = assigned?.TeacherId ?? Guid.Empty;
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

    private async void AssignClassTeacher_Click(object sender, RoutedEventArgs e)
    {
        var schoolClass = SelectedSchoolClass();
        if (schoolClass is null || ClassTeacherCombo.SelectedValue is not Guid teacherId)
        {
            StatusText.Text = "Sınıf öğretmeni atamak için tek bir sınıf ve öğretmen seçin.";
            return;
        }

        await App.Dashboard.Repository.SaveClassTeacherAssignmentAsync(new ClassTeacherAssignment(schoolClass.Id, teacherId, "2025/2026"));
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();
        SyncClassTeacherPanel();
        StatusText.Text = $"{schoolClass.Name} sınıf öğretmeni kaydedildi.";
    }

    private async void RemoveClassTeacher_Click(object sender, RoutedEventArgs e)
    {
        var schoolClass = SelectedSchoolClass();
        if (schoolClass is null)
        {
            StatusText.Text = "Sınıf öğretmeni atamasını kaldırmak için tek bir sınıf seçin.";
            return;
        }

        await App.Dashboard.Repository.DeleteClassTeacherAssignmentAsync(schoolClass.Id);
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();
        SyncClassTeacherPanel();
        StatusText.Text = $"{schoolClass.Name} sınıf öğretmeni ataması kaldırıldı.";
    }

    private SchoolClass? SelectedSchoolClass() =>
        _classes.FirstOrDefault(x => string.Equals(x.Name, EntityPicker.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase));

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

    private void ExportDoorLists_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "HTML dosyası|*.html", FileName = "sinif-kapi-listeleri.html" };
        if (dialog.ShowDialog() != true) return;

        File.WriteAllText(dialog.FileName, BuildDoorListsHtml(), new UTF8Encoding(false));
        StatusText.Text = "Kapıya asılacak sınıf haftalık programları HTML olarak hazırlandı. Tarayıcıdan yazdırıp PDF alabilirsiniz.";
    }

    private string BuildDoorListsHtml()
    {
        var rows = _assignments
            .Select(x => (Assignment: x, Request: RequestFor(x)))
            .Where(x => x.Request is not null)
            .ToArray();

        var byClass = rows.GroupBy(x => x.Request!.Class.Name, StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Sınıf Haftalık Programları</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#111827}.page{page-break-after:always}h1{font-size:26px;margin:0 0 4px}.meta{margin:0 0 14px;color:#475569}table{width:100%;border-collapse:collapse;table-layout:fixed}th,td{border:1px solid #94a3b8;padding:7px;vertical-align:top;font-size:12px}th{background:#e2e8f0}.hour{width:52px;text-align:center;font-weight:700}.lesson{font-weight:700}.teacher{color:#334155;margin-top:3px}.resource{color:#64748b;margin-top:2px}@media print{body{margin:12mm}.page{page-break-after:always}}</style>");
        sb.AppendLine("</head><body>");

        foreach (var group in byClass)
        {
            var schoolClass = _classes.FirstOrDefault(x => string.Equals(x.Name, group.Key, StringComparison.OrdinalIgnoreCase));
            sb.AppendLine("<section class=\"page\">");
            sb.AppendLine($"<h1>{Html(group.Key)} Haftalık Ders Programı</h1>");
            sb.AppendLine($"<p class=\"meta\">2025/2026 · Sınıf öğretmeni: {Html(AdvisorName(schoolClass?.Id))}</p>");
            sb.AppendLine("<table><thead><tr><th class=\"hour\">Saat</th><th>Pazartesi</th><th>Salı</th><th>Çarşamba</th><th>Perşembe</th><th>Cuma</th></tr></thead><tbody>");
            for (var hour = 1; hour <= 10; hour++)
            {
                sb.AppendLine($"<tr><td class=\"hour\">{hour}</td>");
                foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
                {
                    var cell = group
                        .Where(x => x.Assignment.Day == day && hour >= x.Assignment.LessonNumber && hour < x.Assignment.LessonNumber + x.Assignment.BlockLength)
                        .Select(x => $"<div class=\"lesson\">{Html(x.Request!.Course.Name)}</div><div class=\"teacher\">{Html(TeacherNameFor(x.Assignment))}</div><div class=\"resource\">{Html(x.Request.Resource?.Name ?? "-")}</div>")
                        .DefaultIfEmpty("")
                        .Aggregate((a, b) => string.IsNullOrEmpty(a) ? b : a + "<hr>" + b);
                    sb.AppendLine($"<td>{cell}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table></section>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string AdvisorName(Guid? classId)
    {
        if (classId is null) return "Atanmadı";
        var assignment = _classTeachers.FirstOrDefault(x => x.ClassId == classId.Value);
        return assignment is null ? "Atanmadı" : _teachers.FirstOrDefault(x => x.Id == assignment.TeacherId)?.FullName ?? "Atanmadı";
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

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
    private sealed record TeacherItem(Guid Id, string Name);
}
