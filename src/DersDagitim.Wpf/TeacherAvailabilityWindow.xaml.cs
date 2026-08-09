using System.Windows;
using System.Windows.Controls;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class TeacherAvailabilityWindow : Window
{
    private IReadOnlyList<Teacher> _teachers = Array.Empty<Teacher>();
    private IReadOnlyList<AvailabilityRestriction> _restrictions = Array.Empty<AvailabilityRestriction>();

    public TeacherAvailabilityWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _teachers = await App.Dashboard.Repository.GetTeachersAsync();
        _restrictions = await App.Dashboard.Repository.GetAvailabilityRestrictionsAsync();
        FillEditors();
        RenderRows();
    }

    private void FillEditors()
    {
        TeacherCombo.ItemsSource = _teachers.OrderBy(x => x.FullName).Select(x => new TeacherItem(x.Id, x.FullName)).ToArray();
        TeacherCombo.DisplayMemberPath = nameof(TeacherItem.Name);
        TeacherCombo.SelectedValuePath = nameof(TeacherItem.Id);
        TeacherCombo.SelectedIndex = TeacherCombo.Items.Count > 0 ? 0 : -1;

        DayCombo.ItemsSource = new[]
        {
            new DayItem("Pazartesi", DayOfWeek.Monday),
            new DayItem("Salı", DayOfWeek.Tuesday),
            new DayItem("Çarşamba", DayOfWeek.Wednesday),
            new DayItem("Perşembe", DayOfWeek.Thursday),
            new DayItem("Cuma", DayOfWeek.Friday)
        };
        DayCombo.DisplayMemberPath = nameof(DayItem.Name);
        DayCombo.SelectedValuePath = nameof(DayItem.Day);
        DayCombo.SelectedIndex = 0;

        LessonCombo.ItemsSource = Enumerable.Range(1, 10).ToArray();
        LessonCombo.SelectedIndex = 0;

        StatusCombo.ItemsSource = new[]
        {
            new StatusItem("Kesin kilit", RestrictionType.Unavailable, AvailabilitySeverity.HardLock),
            new StatusItem("Tercih edilen boşluk", RestrictionType.PreferredFree, AvailabilitySeverity.SoftPreference),
            new StatusItem("Düşük öncelik", RestrictionType.PossibleLowPriority, AvailabilitySeverity.LowPriority)
        };
        StatusCombo.DisplayMemberPath = nameof(StatusItem.Name);
        StatusCombo.SelectedIndex = 0;
    }

    private void RenderRows()
    {
        var teacherById = _teachers.ToDictionary(x => x.Id);
        if (_restrictions.Count == 0)
        {
            AvailabilityGrid.ItemsSource = _teachers
                .OrderBy(x => x.FullName)
                .Select(x => new Row(Guid.Empty, x.FullName, "-", "", "Kısıt tanımlı değil", "Bu öğretmen için kesin kilit veya tercih kaydı girilmemiş.", "-"))
                .ToArray();
            SummaryText.Text = $"{_teachers.Count} öğretmen listeleniyor. SQLite'a bağlı haftalık uygunluk kaydı yok; yeni kayıt eklemek için üstteki formu kullanın.";
            return;
        }

        AvailabilityGrid.ItemsSource = _restrictions
            .OrderBy(x => teacherById.TryGetValue(x.TeacherId, out var teacher) ? teacher.FullName : "")
            .ThenBy(x => x.Day)
            .ThenBy(x => x.LessonNumber)
            .Select(x =>
            {
                var name = teacherById.TryGetValue(x.TeacherId, out var teacher) ? teacher.FullName : "Bilinmeyen öğretmen";
                return new Row(x.Id, name, DayName(x.Day), x.LessonNumber.ToString(), StatusText(x), x.ReasonLabel ?? x.Note ?? "-", x.RepeatsWeekly ? "Her hafta" : "Tekil");
            })
            .ToArray();
        SummaryText.Text = $"{_restrictions.Count} SQLite haftalık uygunluk/kilit kaydı listeleniyor. Taslak üretiminde kesin kilitler dikkate alınır.";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TeacherCombo.SelectedValue is not Guid teacherId || DayCombo.SelectedValue is not DayOfWeek day || LessonCombo.SelectedItem is not int lesson || StatusCombo.SelectedItem is not StatusItem status)
        {
            SummaryText.Text = "Kayıt için öğretmen, gün, ders saati ve durum seçin.";
            return;
        }

        var reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? status.Name : ReasonBox.Text.Trim();
        var existing = _restrictions.FirstOrDefault(x => x.TeacherId == teacherId && x.Day == day && x.LessonNumber == lesson);
        var restriction = new AvailabilityRestriction(
            existing?.Id ?? Guid.NewGuid(),
            teacherId,
            day,
            lesson,
            status.Type,
            reason,
            status.Severity,
            RepeatsWeekly: true,
            ReasonLabel: reason);

        await App.Dashboard.Repository.SaveAvailabilityRestrictionAsync(restriction);
        _restrictions = await App.Dashboard.Repository.GetAvailabilityRestrictionsAsync();
        DraftWorkspace.UpdateRestrictions(_restrictions);
        ReasonBox.Clear();
        RenderRows();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (AvailabilityGrid.SelectedItem is not Row row || row.Id == Guid.Empty)
        {
            SummaryText.Text = "Silmek için gerçek bir uygunluk/kilit kaydı seçin.";
            return;
        }

        await App.Dashboard.Repository.DeleteAvailabilityRestrictionAsync(row.Id);
        _restrictions = await App.Dashboard.Repository.GetAvailabilityRestrictionsAsync();
        DraftWorkspace.UpdateRestrictions(_restrictions);
        RenderRows();
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

    private sealed record TeacherItem(Guid Id, string Name);
    private sealed record DayItem(string Name, DayOfWeek Day);
    private sealed record StatusItem(string Name, RestrictionType Type, AvailabilitySeverity Severity);
    private sealed record Row(Guid Id, string TeacherName, string Day, string LessonNumber, string Status, string Reason, string Recurrence);
}
