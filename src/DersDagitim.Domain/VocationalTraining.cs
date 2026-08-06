namespace DersDagitim.Domain;
public sealed record WorkplaceVocationalTrainingAssignment(Guid Id, Guid TeacherId, string ClassName, string Department, string Branch, string? WorkplaceGroup, int WeeklyHours, string AdministrativeDecisionNote, string TermOrAcademicYear, bool IsActive, bool IsDemo = true);
