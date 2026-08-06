using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public static class DraftWorkspace
{
    public static int ImportedLessonCount { get; private set; }
    public static int ImportedCardCount { get; private set; }
    public static int MappedLessonCount { get; private set; }
    public static int ProtectedCardCount { get; private set; }
    public static DraftScheduleResult? Current { get; private set; }
    public static IReadOnlyList<LessonRequest> Requests { get; private set; } = Array.Empty<LessonRequest>();
    public static IReadOnlyList<AvailabilityRestriction> Restrictions { get; private set; } = Array.Empty<AvailabilityRestriction>();

    public static async Task<DraftScheduleResult> GenerateAsync(ISchoolRepository repository, bool includeProtectedCards)
    {
        var input = await repository.GetAscSolverInputAsync(includeProtectedCards);
        if (input.Requests.Count == 0) throw new InvalidOperationException("ASC XML ders talepleri bulunamadı veya temel ilişkiler eşleşmedi.");
        ImportedLessonCount = input.ImportedLessonCount; ImportedCardCount = input.ImportedCardCount; MappedLessonCount = input.MappedLessonCount; ProtectedCardCount = input.MappedCardCount;
        Requests = input.Requests; Restrictions = Array.Empty<AvailabilityRestriction>();
        Current = new DraftScheduleSolver().Solve(Requests, input.ProtectedCards, Restrictions); return Current;
    }

    public static async Task<DraftScheduleResult> GenerateDemoAsync(ISchoolRepository repository)
    {
        var classes = await repository.GetClassesAsync(); var teachers = await repository.GetTeachersAsync(); var courses = await repository.GetCoursesAsync(); var resources = await repository.GetResourcesAsync();
        var requests = courses.Select((course, index) => new LessonRequest(Guid.NewGuid(), classes[index % classes.Count], course, teachers[index % teachers.Count], course.IsPractical ? resources.FirstOrDefault() : null, Math.Min(course.WeeklyHours, 10), course.BlockOptions)).ToArray();
        var restrictions = new[] { new AvailabilityRestriction(Guid.NewGuid(), teachers[0].Id, DayOfWeek.Friday, 6, RestrictionType.Unavailable, "DEMO · lisansüstü eğitim", AvailabilitySeverity.HardLock) };
        Current = new DraftScheduleSolver().Solve(requests, Array.Empty<LessonAssignment>(), restrictions); Requests = requests; Restrictions = restrictions; return Current;
    }
    public static (bool Success, string Message) Move(Guid assignmentId, int newLesson, DayOfWeek? newDay = null)
    {
        if (Current is null) return (false, "Önce taslak üretin."); var old = Current.Assignments.FirstOrDefault(x => x.Id == assignmentId); if (old is null) return (false, "Atama bulunamadı."); var request = Requests.FirstOrDefault(x => x.Class.Id == old.ClassId && x.Course.Id == old.CourseId); if (request is null) return (false, "Atama talebi bulunamadı."); var targetDay = newDay ?? old.Day; var slots = Enumerable.Range(newLesson, old.BlockLength).ToArray(); if (slots.Any(x => x > 10)) return (false, "Blok 10. ders saatini aşamaz."); if (slots.Any(x => Restrictions.Any(r => r.TeacherId == old.TeacherId && r.Day == targetDay && r.LessonNumber == x && (r.Severity == AvailabilitySeverity.HardLock || r.Type == RestrictionType.Unavailable)))) return (false, "Öğretmenin kesin kilidi bu slota izin vermiyor."); if (Current.Assignments.Any(x => x.Id != old.Id && x.Day == targetDay && slots.Any(s => s >= x.LessonNumber && s < x.LessonNumber + x.BlockLength) && (x.ClassId == old.ClassId || x.TeacherId == old.TeacherId))) return (false, "Sınıf veya öğretmen çakışması oluşuyor."); var moved = old with { Day = targetDay, LessonNumber = newLesson, IsManual = true }; Current = Current with { Assignments = Current.Assignments.Select(x => x.Id == old.Id ? moved : x).ToArray() }; return (true, "Atama manuel olarak taşındı ve sonraki taslaklarda korunacak.");
    }
    public static void Remove(Guid assignmentId) { if (Current is null) return; Current = Current with { Assignments = Current.Assignments.Where(x => x.Id != assignmentId).ToArray() }; }
}
