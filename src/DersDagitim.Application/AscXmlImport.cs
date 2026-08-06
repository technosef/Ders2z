using System.Xml.Linq;
using DersDagitim.Domain;

namespace DersDagitim.Application;
public sealed record AscXmlNode(string Section, string ExternalId, string Name, string RawXml);
public sealed record AscGroupRecord(string Id, string Name, string ClassId, string StudentIds, bool EntireClass, string DivisionTag);
public sealed record AscLessonRecord(string Id, string ClassIds, string SubjectId, string TeacherIds, string ClassroomIds, string GroupIds, string PeriodsPerCard, string PeriodsPerWeek, string DaysDefId, string WeeksDefId, string TermsDefId);
public sealed record AscCardRecord(string LessonId, string ClassroomIds, string Period, string Weeks, string Terms, string Days);
public sealed record AscXmlPreview(string SourcePath, string DisplayName, int Periods, int Breaks, int Subjects, int Teachers, int Rooms, int Classes, int Groups, int Lessons, int Cards, IReadOnlyList<string> Warnings, IReadOnlyList<AscXmlNode> Nodes, IReadOnlyList<AscGroupRecord> GroupRecords, IReadOnlyList<AscLessonRecord> LessonRecords, IReadOnlyList<AscCardRecord> CardRecords);
public static class AscXmlParser
{
    public static AscXmlPreview Parse(string path)
    {
        using var stream = File.OpenRead(path); var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace); var root = doc.Root ?? throw new InvalidDataException("ASC XML kökü bulunamadı.");
        var nodes = new List<AscXmlNode>(); void Add(string section, string element) { foreach (var x in root.Descendants(element)) nodes.Add(new(section, (string?)x.Attribute("id") ?? $"{section}-{nodes.Count + 1}", (string?)x.Attribute("name") ?? (string?)x.Attribute("short") ?? element, x.ToString(SaveOptions.DisableFormatting))); }
        Add("Dönem", "period"); Add("Ara", "break"); Add("Ders", "subject"); Add("Öğretmen", "teacher"); Add("Oda", "classroom"); Add("Bina", "building"); Add("Sınıf", "class"); Add("Grup", "group"); Add("Ders talebi", "lesson"); Add("Kart / yerleşim", "card"); Add("Gün tanımı", "daysdef"); Add("Hafta tanımı", "weeksdef"); Add("Dönem tanımı", "termsdef");
        var warnings = new List<string>(); var known = new HashSet<string>(new[] { "periods", "breaks", "daysdefs", "weeksdefs", "termsdefs", "subjects", "teachers", "buildings", "classrooms", "grades", "classes", "groups", "lessons", "cards", "classroomsupervisions" }); foreach (var child in root.Elements().Where(x => !known.Contains(x.Name.LocalName))) warnings.Add($"Desteklenmeyen ASC bölümü korunmadı: {child.Name.LocalName} (ham XML içinde bölüm kaydı olarak saklanması sonraki aşamadır).");
        warnings.Add("Ders blokları, kısıtlar ve kartlardaki tüm ASC öznitelikleri ham XML olarak korunur; çözücü eşlemesi ilk sürümde ayrıca doğrulanmalıdır.");
        var groups = root.Descendants("group").Select(x => new AscGroupRecord((string?)x.Attribute("id") ?? "", (string?)x.Attribute("name") ?? "", (string?)x.Attribute("classid") ?? "", (string?)x.Attribute("studentids") ?? "", (string?)x.Attribute("entireclass") == "1", (string?)x.Attribute("divisiontag") ?? "")).ToArray();
        var lessons = root.Descendants("lesson").Select(x => new AscLessonRecord((string?)x.Attribute("id") ?? "", (string?)x.Attribute("classids") ?? "", (string?)x.Attribute("subjectid") ?? "", (string?)x.Attribute("teacherids") ?? "", (string?)x.Attribute("classroomids") ?? "", (string?)x.Attribute("groupids") ?? "", (string?)x.Attribute("periodspercard") ?? "1", (string?)x.Attribute("periodsperweek") ?? "1", (string?)x.Attribute("daysdefid") ?? "", (string?)x.Attribute("weeksdefid") ?? "", (string?)x.Attribute("termsdefid") ?? "")).ToArray();
        var cards = root.Descendants("card").Select(x => new AscCardRecord((string?)x.Attribute("lessonid") ?? "", (string?)x.Attribute("classroomids") ?? "", (string?)x.Attribute("period") ?? "", (string?)x.Attribute("weeks") ?? "", (string?)x.Attribute("terms") ?? "", (string?)x.Attribute("days") ?? "")).ToArray();
        return new(path, (string?)root.Attribute("displayname") ?? "ASC XML", nodes.Count(n => n.Section == "Dönem"), nodes.Count(n => n.Section == "Ara"), nodes.Count(n => n.Section == "Ders"), nodes.Count(n => n.Section == "Öğretmen"), nodes.Count(n => n.Section == "Oda"), nodes.Count(n => n.Section == "Sınıf"), groups.Length, lessons.Length, cards.Length, warnings, nodes, groups, lessons, cards);
    }
}
public static class AscXmlCoreMapping
{
    public static IReadOnlyList<Teacher> Teachers(AscXmlPreview p) => p.Nodes.Where(x => x.Section == "Öğretmen").Select(x => { var e = XElement.Parse(x.RawXml); return new Teacher(Guid.NewGuid(), (string?)e.Attribute("name") ?? x.Name, 30, Code: (string?)e.Attribute("short"), IsDemo: false, StaffStatus: "Kurum verisi", SourceLabel: "ASC XML", SourceUrl: p.SourcePath, SourceAccessDate: DateOnly.FromDateTime(File.GetLastWriteTimeUtc(p.SourcePath).ToLocalTime()), ColorCode: (string?)e.Attribute("color")); }).ToArray();
    public static IReadOnlyList<SchoolClass> Classes(AscXmlPreview p) => p.Nodes.Where(x => x.Section == "Sınıf").Select(x => { var e = XElement.Parse(x.RawXml); var name = (string?)e.Attribute("name") ?? x.Name; var grade = int.TryParse(name.Split('/').FirstOrDefault(), out var g) ? g : 0; return new SchoolClass(Guid.NewGuid(), name, grade, name.Contains('/') ? name.Split('/')[1] : name, IsDemo: false); }).ToArray();
    public static IReadOnlyList<Course> Courses(AscXmlPreview p) => p.Nodes.Where(x => x.Section == "Ders").Select(x => { 
        var e = XElement.Parse(x.RawXml); 
        var name = (string?)e.Attribute("name") ?? x.Name;
        var courseType = GetCourseType(name);
        var isElective = courseType == CourseType.Seçmeli || name.Contains("Seçmeli", StringComparison.OrdinalIgnoreCase);
        return new Course(Guid.NewGuid(), name, 1, new[] { "1" }, Abbreviation: (string?)e.Attribute("short"), 
            SourceLabel: "ASC XML", SourceVersion: p.DisplayName, IsDemo: false, Type: courseType, IsElective: isElective); 
    }).ToArray();
    
    private static CourseType GetCourseType(string name)
    {
        if (name.Contains("Kurs", StringComparison.OrdinalIgnoreCase)) return CourseType.Kurs;
        if (name.Contains("DYK", StringComparison.OrdinalIgnoreCase)) return CourseType.DYK;
        if (name.Contains("Seçmeli", StringComparison.OrdinalIgnoreCase)) return CourseType.Seçmeli;
        return CourseType.Zorunlu;
    }
    public static IReadOnlyList<Resource> Resources(AscXmlPreview p) => p.Nodes.Where(x => x.Section == "Oda").Select(x => { var e = XElement.Parse(x.RawXml); return new Resource(Guid.NewGuid(), (string?)e.Attribute("name") ?? x.Name, int.TryParse((string?)e.Attribute("capacity"), out var c) ? c : 1, ResourceType.Classroom); }).ToArray();
}
