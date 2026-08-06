namespace DersDagitim.Domain;
public sealed record TeacherDuty(string Name, string Details);
public sealed record TeacherAssignmentSummary(string CourseName, int WeeklyHours, IReadOnlyList<string> ClassNames);
public sealed record TeacherWeeklyLoadReport(string SchoolName, string AcademicYear, Teacher Teacher, IReadOnlyList<TeacherDuty> Duties, IReadOnlyList<TeacherAssignmentSummary> Assignments, IReadOnlyList<WorkplaceVocationalTrainingAssignment>? VocationalTraining = null)
{
    public int TotalHours => Assignments.Sum(x => x.WeeklyHours) + (VocationalTraining ?? Array.Empty<WorkplaceVocationalTrainingAssignment>()).Where(x => x.IsActive).Sum(x => x.WeeklyHours);
}
public static class TeacherReportSample
{
    public static TeacherWeeklyLoadReport Create() { var teacher = new Teacher(Guid.NewGuid(), "Mehmet Akif Sönmez", 30, Code: "MAS"); return new("Özel Tokat Dinamik Mesleki ve Teknik Anadolu Lisesi", "2025/2026", teacher, new[] { new TeacherDuty("Sınıf Öğretmenliği", "11/D(B)"), new TeacherDuty("Sosyal Kulübü", "Bilişim Kulübü"), new TeacherDuty("Nöbet Günü ve Yeri", "Perşembe · Bina 1"), new TeacherDuty("Diğer Bilgiler", "") }, new[] { new TeacherAssignmentSummary("Mobil Uygulamalar",20,new[]{"11/D(B)","11/E(B)","11/F(B)","11/G(B)"}),new TeacherAssignmentSummary("Seçmeli Dijital Tasarım",2,new[]{"12/D(B)"}),new TeacherAssignmentSummary("Staj",4,Array.Empty<string>()),new TeacherAssignmentSummary("Bilişim Üzümre",1,Array.Empty<string>()) }, new[] { new WorkplaceVocationalTrainingAssignment(Guid.NewGuid(), teacher.Id, "12/C", "Bilişim Teknolojileri", "Bilişim Teknolojileri", "DEMO · İşletme grubu A", 4, "DEMO · İdari karar notu girilecek", "2025/2026", true) }); }
}
