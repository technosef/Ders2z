namespace DersDagitim.Domain;
public sealed record LessonTime(int LessonNumber, string Start, string End);
public sealed record LaboratoryScheduleCell(string DayName, string ClassName, int StartLesson, int LessonSpan, string CourseAbbreviation, string TeacherCode, bool IsAvailable = false)
{
    public int EndLesson => StartLesson + LessonSpan - 1;
    public string LessonRange => LessonSpan == 1 ? StartLesson.ToString() : $"{StartLesson}-{EndLesson}";
    public string DisplayText => IsAvailable ? "Boş · uygun" : $"{ClassName}  {CourseAbbreviation} + {TeacherCode}";
}
public static class LaboratoryScheduleSample
{
    public const string ResourceName = "Bilişim Laboratuvarı 3";
    public static IReadOnlyList<LessonTime> Times { get; } = new LessonTime[] { new(1,"09:00","09:40"),new(2,"09:50","10:30"),new(3,"10:40","11:20"),new(4,"11:30","12:10"),new(5,"13:00","13:40"),new(6,"13:50","14:30"),new(7,"14:40","15:20"),new(8,"15:30","16:10"),new(9,"16:20","17:00"),new(10,"17:10","17:50") };
    public static IReadOnlyList<LaboratoryScheduleCell> Cells { get; } = new LaboratoryScheduleCell[] { new("Pazartesi","11/E",6,4,"PROG","ŞV"),new("Pazartesi","11/G",6,4,"S.TSRM","HK"),new("Pazartesi","12/C",1,4,"WEB","ÖÖ"),new("Pazartesi","12/D",1,4,"GRAFİK","ÖÖ"),new("Çarşamba","11/E",1,4,"RVK","OD"),new("Çarşamba","12/C",1,4,"BTU","MG"),new("Çarşamba","12/D",1,4,"MAS","HK"),new("Salı","",1,10,"","",true) };
}
