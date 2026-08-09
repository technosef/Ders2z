using System.Windows;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class ConflictWindow : Window
{
    public ConflictWindow()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var result = DraftWorkspace.Current;
        if (result is null)
        {
            SummaryText.Text = "Taslak üretmeden önce kontrol edilecek sonuç yok.";
            ExplanationText.Text = "Önce Taslak Üret çalıştırılmalı.";
            ConflictsGrid.ItemsSource = Array.Empty<Row>();
            return;
        }

        var rows = BuildConflictRows(result).ToList();
        rows.AddRange(result.Unassigned.Select(x => new Row("Yerleşmeyen talep", "-", "-", x.ClassName, $"{x.CourseName}: {x.Reason}")));

        SummaryText.Text = $"Çakışma: {rows.Count(x => x.Type != "Yerleşmeyen talep")} · Yerleşmeyen talep: {result.Unassigned.Count}";
        ExplanationText.Text = "Bu ekran gerçek sınıf, öğretmen ve kaynak bindirmelerini listeler. Taslak ekranındaki okul geneli ders sayıları çakışma değildir.";
        ConflictsGrid.ItemsSource = rows;
    }

    private static IEnumerable<Row> BuildConflictRows(DraftScheduleResult result)
    {
        var requestByKey = DraftWorkspace.Requests
            .GroupBy(x => (x.Class.Id, x.Course.Id))
            .ToDictionary(x => x.Key, x => x.First());

        for (var slot = 1; slot <= 10; slot++)
        {
            foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            {
                var active = result.Assignments
                    .Where(x => x.Day == day && slot >= x.LessonNumber && slot < x.LessonNumber + x.BlockLength)
                    .ToArray();

                foreach (var group in active.GroupBy(x => x.ClassId).Where(x => x.Count() > 1))
                    yield return new Row("Sınıf çakışması", DayName(day), slot.ToString(), NameForClass(group.Key), Describe(group, requestByKey));

                foreach (var group in active.GroupBy(x => x.TeacherId).Where(x => x.Count() > 1))
                    yield return new Row("Öğretmen çakışması", DayName(day), slot.ToString(), NameForTeacher(group.Key), Describe(group, requestByKey));

                foreach (var group in active.Where(x => x.ResourceId is not null).GroupBy(x => x.ResourceId!.Value))
                {
                    var capacity = CapacityForResource(group.Key);
                    if (group.Count() > capacity)
                        yield return new Row("Kaynak kapasitesi", DayName(day), slot.ToString(), NameForResource(group.Key), $"{group.Count()} eşzamanlı kullanım / kapasite {capacity}. {Describe(group, requestByKey)}");
                }
            }
        }
    }

    private static string Describe(IEnumerable<LessonAssignment> assignments, Dictionary<(Guid ClassId, Guid CourseId), LessonRequest> requestByKey) =>
        string.Join(" | ", assignments.Select(x =>
        {
            var key = (x.ClassId, x.CourseId);
            return requestByKey.TryGetValue(key, out var req) ? $"{req.Class.Name} - {req.Course.Name} - {NameForTeacher(x.TeacherId)}" : x.Id.ToString();
        }));

    private static string NameForClass(Guid id) => DraftWorkspace.Requests.FirstOrDefault(x => x.Class.Id == id)?.Class.Name ?? id.ToString();
    private static string NameForTeacher(Guid id) => DraftWorkspace.Teachers.FirstOrDefault(x => x.Id == id)?.FullName ?? DraftWorkspace.Requests.FirstOrDefault(x => x.Teacher.Id == id)?.Teacher.FullName ?? id.ToString();
    private static string NameForResource(Guid id) => DraftWorkspace.Requests.FirstOrDefault(x => x.Resource?.Id == id)?.Resource?.Name ?? id.ToString();
    private static int CapacityForResource(Guid id) => Math.Max(1, DraftWorkspace.Requests.FirstOrDefault(x => x.Resource?.Id == id)?.Resource?.Capacity ?? 1);

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        _ => day.ToString()
    };

    private sealed record Row(string Type, string Day, string Slot, string Entity, string Description);
}
