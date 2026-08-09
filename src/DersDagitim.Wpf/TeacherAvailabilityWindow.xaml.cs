using System.Windows;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class TeacherAvailabilityWindow : Window
{
    public TeacherAvailabilityWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var teachers = await App.Dashboard.Repository.GetTeachersAsync();
        var restrictions = DraftWorkspace.Restrictions;

        if (restrictions.Count == 0)
        {
            AvailabilityGrid.ItemsSource = teachers
                .OrderBy(x => x.FullName)
                .Select(x => new Row(x.FullName, "-", "", "Kısıt tanımlı değil", "Bu öğretmen için kesin kilit veya tercih kaydı girilmemiş.", "-"))
                .ToArray();
            SummaryText.Text = $"{teachers.Count} öğretmen listeleniyor. Henüz SQLite'a bağlı haftalık uygunluk/kilit kaydı yok; bu ekran boş görünmesin diye gerçek öğretmenler durum satırıyla gösteriliyor.";
            return;
        }

        var teacherById = teachers.ToDictionary(x => x.Id);
        AvailabilityGrid.ItemsSource = restrictions
            .OrderBy(x => teacherById.TryGetValue(x.TeacherId, out var teacher) ? teacher.FullName : "")
            .ThenBy(x => x.Day)
            .ThenBy(x => x.LessonNumber)
            .Select(x =>
            {
                var name = teacherById.TryGetValue(x.TeacherId, out var teacher) ? teacher.FullName : "Bilinmeyen öğretmen";
                return new Row(name, DayName(x.Day), x.LessonNumber.ToString(), StatusText(x), x.ReasonLabel ?? x.Note ?? "-", x.RepeatsWeekly ? "Her hafta" : "Tekil");
            })
            .ToArray();
        SummaryText.Text = $"{restrictions.Count} uygunluk/kilit kaydı listeleniyor.";
    }

    private static string StatusText(AvailabilityRestriction restriction) =>
        restriction.Severity == AvailabilitySeverity.HardLock || restriction.Type == RestrictionType.Unavailable
            ? "Kesin kilit"
            : restriction.Type == RestrictionType.PreferredFree ? "Tercih edilen boşluk" : "Düşük öncelik";

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        _ => day.ToString()
    };

    private sealed record Row(string TeacherName, string Day, string LessonNumber, string Status, string Reason, string Recurrence);
}
