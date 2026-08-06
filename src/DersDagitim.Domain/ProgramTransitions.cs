namespace DersDagitim.Domain;
public enum TransitionApprovalStatus { Taslak, UygunlukBekliyor, Onaylandi, Reddedildi }
public sealed record StudentProgramTransition(string CohortName, SchoolProgramType SourceProgram, SchoolProgramType TargetProgram, int TransitionGrade, string AcademicYear, TransitionApprovalStatus ApprovalStatus, string CurriculumVersion, IReadOnlyList<string> MissingOrCompensatoryCourses, string EligibilityRuleCode, string? ApprovalReference = null, string? SourceSystem = null);
public sealed record ProgramTransitionRule(string Code, string Name, string Description, bool IsEnabled, string ConfigurationJson, string SourceLabel, string SourceVersion);
