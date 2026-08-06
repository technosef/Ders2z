namespace DersDagitim.Domain;
public sealed record SchoolClass(Guid Id, string Name, int Grade, string Branch, string? Department = null, bool IsDemo = true, SchoolProgramType ProgramType = SchoolProgramType.AnadoluMeslekProgrami);
public enum SchoolProgramType { AnadoluMeslekProgrami, AnadoluTeknikProgrami }
public sealed record Teacher(Guid Id, string FullName, int WeeklyMaximumHours, int? PreferredDayOff = null, string? Code = null, bool IsDemo = true, string? Department = null, string? AvailabilityNote = null, string StaffStatus = "Kadrolu", string? SourceLabel = null, string? SourceUrl = null, DateOnly? SourceAccessDate = null, string? ColorCode = null);
public enum CourseType { Zorunlu, Seçmeli, Kurs, DYK }
public sealed record Course(Guid Id, string Name, int WeeklyHours, IReadOnlyList<string> BlockOptions, bool IsPractical = false, string? Abbreviation = null, string? SourceLabel = null, string? SourceVersion = null, bool IsDemo = true, CourseType Type = CourseType.Zorunlu, bool IsElective = false);
public sealed record CurriculumImportProfile(string Department, string Branch, int Grade, SchoolProgramType ProgramType, string AcademicYearVersion, string SourceLabel);
public sealed record Resource(Guid Id, string Name, int Capacity, ResourceType Type);
public enum ResourceType { Classroom, Workshop, Laboratory }
public sealed record GuidanceDuty(Guid Id, string Name, int WeeklyHours, Guid? TeacherId = null);
public sealed record TimeSlotProfile(Guid Id, string Name, int DaysPerWeek, int LessonsPerDay, int LunchAfterLesson, IReadOnlyList<DayOfWeek> TeachingDays);
public sealed record SchedulingRule(Guid Id, string Code, string DisplayName, string Description, bool IsEnabled, string ConfigurationJson);
public sealed record AvailabilityRestriction(Guid Id, Guid TeacherId, DayOfWeek Day, int LessonNumber, RestrictionType Type, string? Note = null, AvailabilitySeverity Severity = AvailabilitySeverity.HardLock, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, bool RepeatsWeekly = true, string? ReasonLabel = null);
public enum RestrictionType { Unavailable, PreferredFree, PossibleLowPriority }
public enum AvailabilitySeverity { HardLock, SoftPreference, LowPriority }
public sealed record TeacherAvailabilityProfile(Guid TeacherId, string? ReasonLabel, string? ReasonNote, IReadOnlyList<AvailabilityRestriction> Restrictions);
public sealed record SchedulingPreference(bool ConsecutivePracticalLessons, bool TheoryBeforePractice, bool PracticeBeforeTheory);
public sealed record LessonAssignment(Guid Id, Guid ClassId, Guid CourseId, Guid TeacherId, Guid? ResourceId, DayOfWeek Day, int LessonNumber, int BlockLength = 1, bool IsManual = false);
public sealed class SchoolSettings
{
    public TimeSlotProfile DefaultProfile { get; init; } = CreateProfile("Varsayılan (5 gün / 10 ders)");
    public TimeSlotProfile UpperGradesProfile { get; init; } = CreateProfile("11-12. sınıflar");
    public TimeSlotProfile LowerGradesProfile { get; init; } = CreateProfile("9-10. sınıflar");
    public SchedulingPreference Preferences { get; init; } = new(true, true, false);
    private static TimeSlotProfile CreateProfile(string name) => new(Guid.NewGuid(), name, 5, 10, 5, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
}
