using DersDagitim.Domain;
namespace DersDagitim.Application;
public sealed record DataStatus(string Source, string LastImport, int Teachers, int Courses, int Classes, int Resources, int Groups, int Lessons, int Cards, int DemoRecords, string Verification);
public sealed record AscSolverInput(
    IReadOnlyList<LessonRequest> Requests,
    IReadOnlyList<LessonAssignment> ProtectedCards,
    IReadOnlyDictionary<Guid, long> ProtectedCardIds,
    IReadOnlyDictionary<string, LessonRequest> RequestsByExternalLessonId,
    int ImportedLessonCount,
    int ImportedCardCount,
    int MappedLessonCount,
    int MappedCardCount);
public sealed record AscScheduleCard(long CardId, string LessonId, string ClassName, string CourseName, string TeacherName, string ResourceName, string TeacherColor, string DayName, int Period, int BlockLength, bool IsManualOverride, bool IsRemoved);
public sealed record AscCardOverride(long CardId, int Period, string Days, bool IsRemoved);
public sealed record AscCardTeacherOverride(long CardId, Guid TeacherId);
public sealed record ClassTeacherAssignment(Guid ClassId, Guid TeacherId, string AcademicYear);
public interface ISchoolRepository { Task InitializeAsync(CancellationToken cancellationToken = default); Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default); Task<IReadOnlyList<Teacher>> GetTeachersAsync(CancellationToken cancellationToken = default); Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default); Task<IReadOnlyList<Resource>> GetResourcesAsync(CancellationToken cancellationToken = default); Task<IReadOnlyList<AvailabilityRestriction>> GetAvailabilityRestrictionsAsync(CancellationToken cancellationToken = default); Task SaveAvailabilityRestrictionAsync(AvailabilityRestriction value, CancellationToken cancellationToken = default); Task DeleteAvailabilityRestrictionAsync(Guid id, CancellationToken cancellationToken = default); Task<IReadOnlyList<ClassTeacherAssignment>> GetClassTeacherAssignmentsAsync(CancellationToken cancellationToken = default); Task SaveClassTeacherAssignmentAsync(ClassTeacherAssignment value, CancellationToken cancellationToken = default); Task DeleteClassTeacherAssignmentAsync(Guid classId, CancellationToken cancellationToken = default); Task<AscSolverInput> GetAscSolverInputAsync(bool includeProtectedCards, CancellationToken cancellationToken = default); Task<IReadOnlyList<AscScheduleCard>> GetAscScheduleCardsAsync(CancellationToken cancellationToken = default); Task SaveAscCardOverrideAsync(AscCardOverride value, CancellationToken cancellationToken = default); Task SaveAscCardTeacherOverrideAsync(AscCardTeacherOverride value, CancellationToken cancellationToken = default); Task SaveTeacherAsync(Teacher value, CancellationToken cancellationToken = default); Task ImportTeachersAsync(IReadOnlyList<Teacher> values, CancellationToken cancellationToken = default); Task CleanTeachersForAscAsync(IReadOnlyList<Teacher> xmlTeachers, CancellationToken cancellationToken = default); Task CleanDemoDataAsync(CancellationToken cancellationToken = default); Task ImportAscXmlAsync(AscXmlPreview preview, CancellationToken cancellationToken = default); Task ExportAscXmlAsync(string path, CancellationToken cancellationToken = default); Task SaveClassAsync(SchoolClass value, CancellationToken cancellationToken = default); Task SaveCourseAsync(Course value, CancellationToken cancellationToken = default); Task SaveResourceAsync(Resource value, CancellationToken cancellationToken = default); Task DeleteAsync(string entity, Guid id, CancellationToken cancellationToken = default); }
public sealed class DashboardService(ISchoolRepository repository)
{
    public ISchoolRepository Repository => repository;
    public async Task<DashboardSummary> LoadAsync(CancellationToken cancellationToken = default) { await repository.InitializeAsync(cancellationToken); return new DashboardSummary((await repository.GetClassesAsync(cancellationToken)).Count, (await repository.GetTeachersAsync(cancellationToken)).Count, (await repository.GetCoursesAsync(cancellationToken)).Count); }
}
public sealed record DashboardSummary(int ClassCount, int TeacherCount, int CourseCount);
