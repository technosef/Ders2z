namespace DersDagitim.Domain;

public sealed record TeacherDuty(string Name, string Details);
public sealed record TeacherAssignmentSummary(string CourseName, int WeeklyHours, IReadOnlyList<string> ClassNames);

public sealed record TeacherWeeklyLoadReport(
    string SchoolName,
    string AcademicYear,
    Teacher Teacher,
    IReadOnlyList<TeacherDuty> Duties,
    IReadOnlyList<TeacherAssignmentSummary> Assignments,
    IReadOnlyList<WorkplaceVocationalTrainingAssignment>? VocationalTraining = null)
{
    public int TotalHours => Assignments.Sum(x => x.WeeklyHours) + (VocationalTraining ?? Array.Empty<WorkplaceVocationalTrainingAssignment>()).Where(x => x.IsActive).Sum(x => x.WeeklyHours);
}
