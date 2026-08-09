using System.Windows;
using System.Windows.Controls;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class TeacherReportWindow : Window
{
    private IReadOnlyList<LessonAssignment> _assignments = Array.Empty<LessonAssignment>();
    private IReadOnlyList<LessonRequest> _requests = Array.Empty<LessonRequest>();
    private IReadOnlyList<Teacher> _teachers = Array.Empty<Teacher>();
    private bool _ready;

    public TeacherReportWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _teachers = await App.Dashboard.Repository.GetTeachersAsync();
        if (DraftWorkspace.Current is not null && DraftWorkspace.Requests.Count > 0)
        {
            _assignments = DraftWorkspace.Current.Assignments;
            _requests = DraftWorkspace.Requests;
        }
        else
        {
            var input = await App.Dashboard.Repository.GetAscSolverInputAsync(true);
            _assignments = input.ProtectedCards;
            _requests = input.Requests;
        }

        var teacherIdsWithAssignments = _assignments.Select(x => x.TeacherId).ToHashSet();
        var pickerItems = _teachers
            .Where(x => teacherIdsWithAssignments.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => new TeacherItem(x.Id, x.FullName))
            .ToArray();

        TeacherPicker.ItemsSource = pickerItems;
        TeacherPicker.DisplayMemberPath = nameof(TeacherItem.Name);
        TeacherPicker.SelectedValuePath = nameof(TeacherItem.Id);
        _ready = true;
        TeacherPicker.SelectedIndex = pickerItems.Length > 0 ? 0 : -1;
        if (pickerItems.Length == 0) RenderEmpty();
    }

    private void TeacherPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || TeacherPicker.SelectedItem is not TeacherItem item) return;
        RenderTeacher(item.Id);
    }

    private void RenderTeacher(Guid teacherId)
    {
        var teacher = _teachers.FirstOrDefault(x => x.Id == teacherId);
        if (teacher is null) { RenderEmpty(); return; }

        TeacherText.Text = teacher.FullName;
        var rows = _assignments
            .Where(x => x.TeacherId == teacherId)
            .Select(x => (Assignment: x, Request: FindRequest(x)))
            .Where(x => x.Request is not null)
            .GroupBy(x => x.Request!.Course.Name)
            .Select(x => new AssignmentRow(
                x.Key,
                x.Sum(y => y.Assignment.BlockLength),
                string.Join(", ", x.Select(y => y.Request!.Class.Name).Distinct().OrderBy(y => y))))
            .OrderByDescending(x => x.WeeklyHours)
            .ThenBy(x => x.CourseName)
            .ToArray();

        AssignmentsGrid.ItemsSource = rows;
        DutiesGrid.ItemsSource = new[]
        {
            new InfoRow("Kadro durumu", teacher.StaffStatus),
            new InfoRow("Kod", teacher.Code ?? "-"),
            new InfoRow("Kaynak", teacher.SourceLabel ?? "-"),
            new InfoRow("Renk", teacher.ColorCode ?? "-")
        };
        TotalText.Text = $"{rows.Sum(x => x.WeeklyHours)} saat";
        SummaryText.Text = $"{rows.Length} ders satırı · {rows.Sum(x => x.WeeklyHours)} saat. Veri gerçek ASC atamalarından hesaplandı.";
    }

    private LessonRequest? FindRequest(LessonAssignment assignment) =>
        _requests.FirstOrDefault(x => x.Class.Id == assignment.ClassId && x.Course.Id == assignment.CourseId && x.Teacher.Id == assignment.TeacherId);

    private void RenderEmpty()
    {
        TeacherText.Text = "Ataması olan öğretmen bulunamadı";
        AssignmentsGrid.ItemsSource = Array.Empty<AssignmentRow>();
        DutiesGrid.ItemsSource = Array.Empty<InfoRow>();
        TotalText.Text = "0 saat";
        SummaryText.Text = "Önce ASC XML aktarımı veya Taslak Üret akışı çalışmalıdır.";
    }

    private void Export_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Bu ekran artık gerçek öğretmen ders yükünü gösteriyor. Dosya üretimi ayrı Excel/PDF adımında bağlanacak.", "Dışa aktarım", MessageBoxButton.OK, MessageBoxImage.Information);

    private sealed record TeacherItem(Guid Id, string Name);
    private sealed record AssignmentRow(string CourseName, int WeeklyHours, string ClassNamesText);
    private sealed record InfoRow(string Name, string Details);
}
