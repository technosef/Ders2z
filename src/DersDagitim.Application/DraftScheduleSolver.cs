using DersDagitim.Domain;

namespace DersDagitim.Application;

public sealed record LessonRequest(Guid Id, SchoolClass Class, Course Course, Teacher Teacher, Resource? Resource, int WeeklyHours, IReadOnlyList<string> BlockPatterns);
public sealed record UnassignedLessonReason(Guid RequestId, string ClassName, string CourseName, string Reason);
public sealed record DraftScheduleResult(IReadOnlyList<LessonAssignment> Assignments, IReadOnlyList<UnassignedLessonReason> Unassigned, bool IsDraft = true);

public sealed class DraftScheduleSolver
{
    private static readonly DayOfWeek[] Days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    public DraftScheduleResult Solve(IReadOnlyList<LessonRequest> requests, IReadOnlyList<LessonAssignment> protectedManualAssignments, IReadOnlyList<AvailabilityRestriction> restrictions)
    {
        var assignments = protectedManualAssignments.ToList();
        var rejected = new List<UnassignedLessonReason>();
        foreach (var request in requests)
        {
            var remaining = Math.Max(0, request.WeeklyHours - assignments.Where(x => x.ClassId == request.Class.Id && x.CourseId == request.Course.Id && x.TeacherId == request.Teacher.Id).Sum(x => x.BlockLength));
            var placedAny = remaining == 0;
            foreach (var pattern in request.BlockPatterns.OrderByDescending(ParseBlockLength))
            {
                foreach (var part in pattern.Split('+', StringSplitOptions.RemoveEmptyEntries))
                {
                    var length = ParseBlockLength(part);
                    while (remaining >= length && TryPlace(request, length, assignments, restrictions, out var placed))
                    {
                        assignments.Add(placed); remaining -= length; placedAny = true;
                    }
                }
            }
            if (remaining > 0) rejected.Add(new(request.Id, request.Class.Name, request.Course.Name, placedAny ? $"{remaining} saat için uygun sınıf/öğretmen/kaynak zamanı bulunamadı." : "Blok deseni, çakışma veya öğretmen kesin kilidi nedeniyle yerleştirilemedi."));
        }
        return new DraftScheduleResult(assignments, rejected);
    }

    private static bool TryPlace(LessonRequest request, int length, List<LessonAssignment> assignments, IReadOnlyList<AvailabilityRestriction> restrictions, out LessonAssignment placed)
    {
        foreach (var day in Days)
            for (var lesson = 1; lesson <= 11 - length; lesson++)
            {
                var slots = Enumerable.Range(lesson, length).ToArray();
                if (slots.Any(slot => IsHardLocked(request.Teacher.Id, day, slot, restrictions))) continue;
                if (slots.Any(slot => assignments.Any(x => x.Day == day && x.LessonNumber == slot && (x.ClassId == request.Class.Id || x.TeacherId == request.Teacher.Id)))) continue;
                if (assignments.Where(x => x.TeacherId == request.Teacher.Id).Sum(x => x.BlockLength) + length > request.Teacher.WeeklyMaximumHours) continue;
                if (request.Resource is not null && assignments.Count(x => x.Day == day && x.LessonNumber >= lesson && x.LessonNumber < lesson + length && x.ResourceId == request.Resource.Id) >= request.Resource.Capacity) continue;
                placed = new LessonAssignment(Guid.NewGuid(), request.Class.Id, request.Course.Id, request.Teacher.Id, request.Resource?.Id, day, lesson, length, false);
                return true;
            }
        placed = default!; return false;
    }

    private static bool IsHardLocked(Guid teacherId, DayOfWeek day, int lesson, IReadOnlyList<AvailabilityRestriction> restrictions) => restrictions.Any(x => x.TeacherId == teacherId && x.Day == day && x.LessonNumber == lesson && (x.Severity == AvailabilitySeverity.HardLock || x.Type == RestrictionType.Unavailable));
    private static int ParseBlockLength(string pattern) => int.TryParse(pattern.Split('+')[0], out var length) ? length : pattern.Count(c => c == '+') + 1;
}
