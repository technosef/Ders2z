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
    public static bool CanRestoreLastRemoved => _lastRemoved is not null;

    private static ISchoolRepository? _repository;
    private static IReadOnlyDictionary<Guid, long> _protectedCardIds = new Dictionary<Guid, long>();
    private static (long CardId, int Period, DayOfWeek Day)? _lastRemoved;
    private static bool _includeProtectedCards;

    public static async Task<DraftScheduleResult> GenerateAsync(ISchoolRepository repository, bool includeProtectedCards)
    {
        var input = await repository.GetAscSolverInputAsync(includeProtectedCards);
        if (input.Requests.Count == 0) throw new InvalidOperationException("ASC XML ders talepleri bulunamadı veya temel ilişkiler eşleşmedi.");
        _repository = repository; _includeProtectedCards = includeProtectedCards; _protectedCardIds = input.ProtectedCardIds;
        ImportedLessonCount = input.ImportedLessonCount; ImportedCardCount = input.ImportedCardCount; MappedLessonCount = input.MappedLessonCount; ProtectedCardCount = input.ProtectedCards.Count;
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
    public static async Task<(bool Success, string Message)> MoveAsync(Guid assignmentId, int newLesson, DayOfWeek? newDay = null)
    {
        if (Current is null) return (false, "Önce taslak üretin.");
        var old = Current.Assignments.FirstOrDefault(x => x.Id == assignmentId); if (old is null) return (false, "Atama bulunamadı.");
        var request = Requests.FirstOrDefault(x => x.Class.Id == old.ClassId && x.Course.Id == old.CourseId && x.Teacher.Id == old.TeacherId); if (request is null) return (false, "Atama talebi bulunamadı.");
        var targetDay = newDay ?? old.Day; var slots = Enumerable.Range(newLesson, old.BlockLength).ToArray();
        if (newLesson < 1 || slots.Any(x => x > 10)) return (false, "Blok 1-10 ders saati sınırının dışına taşamaz.");
        if (slots.Any(x => Restrictions.Any(r => r.TeacherId == old.TeacherId && r.Day == targetDay && r.LessonNumber == x && (r.Severity == AvailabilitySeverity.HardLock || r.Type == RestrictionType.Unavailable)))) return (false, "Öğretmenin kesin kilidi bu slota izin vermiyor.");
        var overlapping = Current.Assignments.Where(x => x.Id != old.Id && x.Day == targetDay && slots.Any(s => s >= x.LessonNumber && s < x.LessonNumber + x.BlockLength)).ToArray();
        if (overlapping.Any(x => x.ClassId == old.ClassId)) return (false, "Sınıf çakışması oluşuyor.");
        if (overlapping.Any(x => x.TeacherId == old.TeacherId)) return (false, "Öğretmen çakışması oluşuyor.");
        if (old.ResourceId is not null && overlapping.Count(x => x.ResourceId == old.ResourceId) >= Math.Max(1, request.Resource?.Capacity ?? 1)) return (false, "Derslik/kaynak eşzamanlı kapasitesi aşılıyor.");
        if (!_protectedCardIds.TryGetValue(old.Id, out var cardId) || _repository is null) return (false, "Bu atama ASC kartına bağlı değil; kalıcı manuel değişiklik yalnız mevcut ASC kartlarında yapılabilir.");
        await _repository.SaveAscCardOverrideAsync(new AscCardOverride(cardId, newLesson, DayBits(targetDay), false));
        var moved = old with { Day = targetDay, LessonNumber = newLesson, IsManual = true };
        Current = Current with { Assignments = Current.Assignments.Select(x => x.Id == old.Id ? moved : x).ToArray() };
        return (true, "Atama taşındı, SQLite'a kaydedildi ve sonraki taslak üretiminde korunacak.");
    }

    public static async Task<(bool Success, string Message)> RemoveAsync(Guid assignmentId)
    {
        if (Current is null) return (false, "Önce taslak üretin.");
        var assignment = Current.Assignments.FirstOrDefault(x => x.Id == assignmentId); if (assignment is null) return (false, "Atama bulunamadı.");
        if (!_protectedCardIds.TryGetValue(assignment.Id, out var cardId) || _repository is null) return (false, "Bu atama ASC kartına bağlı değil; kalıcı kaldırma işlemi uygulanmadı.");
        await _repository.SaveAscCardOverrideAsync(new AscCardOverride(cardId, assignment.LessonNumber, DayBits(assignment.Day), true));
        _lastRemoved = (cardId, assignment.LessonNumber, assignment.Day);
        Current = Current with { Assignments = Current.Assignments.Where(x => x.Id != assignmentId).ToArray() };
        _protectedCardIds = _protectedCardIds.Where(x => x.Key != assignmentId).ToDictionary(x => x.Key, x => x.Value);
        return (true, "Atama kaldırıldı ve SQLite'a kaydedildi; sonraki taslak üretiminde yeniden eklenmeyecek.");
    }

    public static async Task<(bool Success, string Message)> RestoreLastRemovedAsync()
    {
        if (_lastRemoved is null || _repository is null) return (false, "Bu oturumda geri alınabilecek kaldırılmış kart yok.");
        var removed = _lastRemoved.Value;
        await _repository.SaveAscCardOverrideAsync(new AscCardOverride(removed.CardId, removed.Period, DayBits(removed.Day), false));
        _lastRemoved = null;
        await GenerateAsync(_repository, _includeProtectedCards);
        return (true, "Son kaldırılan ASC kartı geri alındı ve taslak yenilendi.");
    }

    private static string DayBits(DayOfWeek day)
    {
        var bits = new char[] { '0', '0', '0', '0', '0' };
        var index = (int)day - (int)DayOfWeek.Monday;
        if (index >= 0 && index < bits.Length) bits[index] = '1';
        return new string(bits);
    }
}
