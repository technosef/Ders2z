using DersDagitim.Application;
using DersDagitim.Domain;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Xml.Linq;

namespace DersDagitim.Infrastructure;

public sealed class SqliteSchoolRepository : ISchoolRepository
{
    private readonly string _connectionString; private readonly string _databasePath;
    public SqliteSchoolRepository(string databasePath) { SQLitePCL.Batteries_V2.Init(); _databasePath = databasePath; _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString(); }

    public async Task InitializeAsync(CancellationToken token = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SchoolClasses (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Grade INTEGER NOT NULL, Branch TEXT NOT NULL, Department TEXT NULL, IsDemo INTEGER NOT NULL DEFAULT 1, ProgramType INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS Teachers (Id TEXT PRIMARY KEY, FullName TEXT NOT NULL, WeeklyMaximumHours INTEGER NOT NULL, PreferredDayOff INTEGER NULL, Code TEXT NULL, IsDemo INTEGER NOT NULL DEFAULT 1, Department TEXT NULL, AvailabilityNote TEXT NULL, StaffStatus TEXT NOT NULL DEFAULT 'Kadrolu', SourceLabel TEXT NULL, SourceUrl TEXT NULL, SourceAccessDate TEXT NULL, ColorCode TEXT NULL);
            CREATE TABLE IF NOT EXISTS Courses (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, WeeklyHours INTEGER NOT NULL, BlockOptions TEXT NOT NULL, IsPractical INTEGER NOT NULL, Abbreviation TEXT NULL, SourceLabel TEXT NULL, SourceVersion TEXT NULL, IsDemo INTEGER NOT NULL DEFAULT 1, CourseType INTEGER NOT NULL DEFAULT 0, IsElective INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS Resources (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Capacity INTEGER NOT NULL, ResourceType INTEGER NOT NULL, IsDemo INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS SchedulingRules (Id TEXT PRIMARY KEY, Code TEXT NOT NULL, DisplayName TEXT NOT NULL, Description TEXT NOT NULL, IsEnabled INTEGER NOT NULL, ConfigurationJson TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS CurriculumImportProfiles (Id TEXT PRIMARY KEY, Department TEXT NOT NULL, Branch TEXT NOT NULL, Grade INTEGER NOT NULL, ProgramType INTEGER NOT NULL, AcademicYearVersion TEXT NOT NULL, SourceLabel TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS StudentProgramTransitions (Id TEXT PRIMARY KEY, CohortName TEXT NOT NULL, SourceProgram INTEGER NOT NULL, TargetProgram INTEGER NOT NULL, TransitionGrade INTEGER NOT NULL, AcademicYear TEXT NOT NULL, ApprovalStatus INTEGER NOT NULL, CurriculumVersion TEXT NOT NULL, MissingOrCompensatoryCourses TEXT NOT NULL, EligibilityRuleCode TEXT NOT NULL, ApprovalReference TEXT NULL, SourceSystem TEXT NULL);
            CREATE TABLE IF NOT EXISTS ProgramTransitionRules (Id TEXT PRIMARY KEY, Code TEXT NOT NULL, Name TEXT NOT NULL, Description TEXT NOT NULL, IsEnabled INTEGER NOT NULL, ConfigurationJson TEXT NOT NULL, SourceLabel TEXT NOT NULL, SourceVersion TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS WorkplaceVocationalTrainingAssignments (Id TEXT PRIMARY KEY, TeacherId TEXT NOT NULL, ClassName TEXT NOT NULL, Department TEXT NOT NULL, Branch TEXT NOT NULL, WorkplaceGroup TEXT NULL, WeeklyHours INTEGER NOT NULL DEFAULT 4, AdministrativeDecisionNote TEXT NOT NULL, TermOrAcademicYear TEXT NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1, IsDemo INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS AscXmlImports (Id TEXT PRIMARY KEY, SourcePath TEXT NOT NULL, DisplayName TEXT NOT NULL, ImportedAt TEXT NOT NULL, IsInstitutionData INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS AscXmlNodes (ImportId TEXT NOT NULL, Section TEXT NOT NULL, ExternalId TEXT NOT NULL, Name TEXT NOT NULL, RawXml TEXT NOT NULL, PRIMARY KEY (ImportId,Section,ExternalId));
            CREATE TABLE IF NOT EXISTS AscGroups (ImportId TEXT NOT NULL, ExternalId TEXT NOT NULL, Name TEXT NOT NULL, ClassId TEXT NOT NULL, StudentIds TEXT NOT NULL, EntireClass INTEGER NOT NULL, DivisionTag TEXT NOT NULL, PRIMARY KEY (ImportId,ExternalId));
            CREATE TABLE IF NOT EXISTS AscLessons (ImportId TEXT NOT NULL, ExternalId TEXT NOT NULL, ClassIds TEXT NOT NULL, SubjectId TEXT NOT NULL, TeacherIds TEXT NOT NULL, ClassroomIds TEXT NOT NULL, GroupIds TEXT NOT NULL, PeriodsPerCard TEXT NOT NULL, PeriodsPerWeek TEXT NOT NULL, DaysDefId TEXT NOT NULL, WeeksDefId TEXT NOT NULL, TermsDefId TEXT NOT NULL, PRIMARY KEY (ImportId,ExternalId));
            CREATE TABLE IF NOT EXISTS AscCards (ImportId TEXT NOT NULL, Id INTEGER PRIMARY KEY AUTOINCREMENT, LessonId TEXT NOT NULL, ClassroomIds TEXT NOT NULL, Period TEXT NOT NULL, Weeks TEXT NOT NULL, Terms TEXT NOT NULL, Days TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS AscCardOverrides (CardId INTEGER PRIMARY KEY, Period INTEGER NOT NULL, Days TEXT NOT NULL, IsRemoved INTEGER NOT NULL DEFAULT 0, UpdatedAt TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS AppFlags (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
            """;
        await command.ExecuteNonQueryAsync(token);
        await EnsureLegacyColumnsAsync(connection, token);
        await EnsureTeacherColorColumnAsync(connection, token);
        await RemoveProvenancePrefixesAsync(connection, token);
        await SeedDemoDataAsync(connection, token);
        await SeedAdditionalDemoDataAsync(connection, token);
    }

    public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken t = default) => ReadAsync("SELECT Id,Name,Grade,Branch,Department,IsDemo,ProgramType FROM SchoolClasses ORDER BY Grade,Branch", r => new SchoolClass(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetInt32(2), r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetInt32(5) == 1, (SchoolProgramType)r.GetInt32(6)), t);
    public Task<IReadOnlyList<Teacher>> GetTeachersAsync(CancellationToken t = default) => ReadAsync("SELECT Id,FullName,WeeklyMaximumHours,PreferredDayOff,Code,IsDemo,Department,AvailabilityNote,StaffStatus,SourceLabel,SourceUrl,SourceAccessDate,ColorCode FROM Teachers ORDER BY FullName", r => new Teacher(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetInt32(2), r.IsDBNull(3) ? null : r.GetInt32(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetInt32(5) == 1, r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? "Kadrolu" : r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9), r.IsDBNull(10) ? null : r.GetString(10), r.IsDBNull(11) ? null : DateOnly.Parse(r.GetString(11)), r.IsDBNull(12) ? null : r.GetString(12)), t);
    public Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken t = default) => ReadAsync("SELECT Id,Name,WeeklyHours,BlockOptions,IsPractical,Abbreviation,SourceLabel,SourceVersion,IsDemo,CourseType,IsElective FROM Courses ORDER BY Name", r => new Course(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetInt32(2), r.GetString(3).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), r.GetInt32(4) == 1, r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7), r.GetInt32(8) == 1, (CourseType)r.GetInt32(9), r.GetInt32(10) == 1), t);
    public Task<IReadOnlyList<Resource>> GetResourcesAsync(CancellationToken t = default) => ReadAsync("SELECT Id,Name,Capacity,ResourceType FROM Resources ORDER BY Name", r => new Resource(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetInt32(2), (ResourceType)r.GetInt32(3)), t);
    public async Task<AscSolverInput> GetAscSolverInputAsync(bool includeProtectedCards, CancellationToken t = default)
    {
        var matchingImport = (await ReadAsync("SELECT l.ImportId FROM AscLessons l INNER JOIN AscXmlNodes n ON n.ImportId=l.ImportId AND n.ExternalId=l.SubjectId GROUP BY l.ImportId ORDER BY (SELECT ImportedAt FROM AscXmlImports i WHERE i.Id=l.ImportId) DESC LIMIT 1", r => r.GetString(0), t)).FirstOrDefault();
        var importFilter = string.IsNullOrWhiteSpace(matchingImport) ? "(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)" : $"'{matchingImport.Replace("'", "''")}'";
        var lessons = await ReadAsync($"SELECT ExternalId,ClassIds,SubjectId,TeacherIds,ClassroomIds,GroupIds,PeriodsPerCard,PeriodsPerWeek,DaysDefId,WeeksDefId,TermsDefId FROM AscLessons WHERE ImportId={importFilter}", r => new AscLessonRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10)), t);
        var cards = await ReadAsync($"SELECT Id,LessonId,ClassroomIds,Period,Weeks,Terms,Days FROM AscCards WHERE ImportId={importFilter} ORDER BY Id", r => (Id: r.GetInt64(0), Record: new AscCardRecord(r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6))), t);
        var overrides = await ReadAsync("SELECT CardId,Period,Days,IsRemoved FROM AscCardOverrides", r => new AscCardOverride(r.GetInt64(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3) == 1), t);
        var overrideById = overrides.ToDictionary(x => x.CardId);
        var groups = await ReadAsync($"SELECT ExternalId,ClassId FROM AscGroups WHERE ImportId={importFilter}", r => (Id: r.GetString(0), ClassId: r.GetString(1)), t);
        var nodes = await ReadAsync($"SELECT ExternalId,RawXml FROM AscXmlNodes WHERE ImportId={importFilter}", r => (Id: r.GetString(0), Raw: r.GetString(1)), t);
        var classes = await GetClassesAsync(t); var teachers = await GetTeachersAsync(t); var courses = await GetCoursesAsync(t); var resources = await GetResourcesAsync(t);
        var nameById = nodes.Select(x => XElement.Parse(x.Raw)).Where(x => x.Attribute("id") is not null).ToDictionary(x => (string)x.Attribute("id")!, x => (string?)x.Attribute("name") ?? (string?)x.Attribute("short") ?? "", StringComparer.OrdinalIgnoreCase);
        var sourcePath = (await ReadAsync("SELECT SourcePath FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1", r => r.GetString(0), t)).FirstOrDefault();
        var sourceCandidates = new[] { sourcePath, @"C:\Users\masco\Desktop\asctt2012 (2) (1).xml" }.Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in sourceCandidates)
        {
            var sourceDocument = XDocument.Load(candidate!); var sourceIds = sourceDocument.Descendants().Select(x => (string?)x.Attribute("id")).Where(x => x is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!lessons.Any(x => sourceIds.Contains(x.SubjectId))) continue;
            foreach (var element in sourceDocument.Descendants().Where(x => x.Attribute("id") is not null)) nameById[(string)element.Attribute("id")!] = (string?)element.Attribute("name") ?? (string?)element.Attribute("short") ?? "";
            break;
        }
        static string[] Ids(string value) => value.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string Name(string id) => nameById.TryGetValue(id, out var value) ? value : id;
        var groupClassById = groups.ToDictionary(x => x.Id, x => x.ClassId, StringComparer.OrdinalIgnoreCase);
        static int Number(string value, int fallback) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? Math.Max(1, (int)Math.Round(n)) : fallback;
        var requests = new List<LessonRequest>(); var requestByExternal = new Dictionary<string, LessonRequest>(StringComparer.OrdinalIgnoreCase); var mapped = 0;
        foreach (var lesson in lessons)
        {
            var classId = Ids(lesson.ClassIds).FirstOrDefault(); if (string.IsNullOrWhiteSpace(classId) && groupClassById.TryGetValue(Ids(lesson.GroupIds).FirstOrDefault() ?? "", out var groupClassId)) classId = groupClassId;
            var className = Name(classId ?? ""); var subjectName = Name(lesson.SubjectId); var teacherName = Name(Ids(lesson.TeacherIds).FirstOrDefault() ?? ""); var resourceName = Name(Ids(lesson.ClassroomIds).FirstOrDefault() ?? "");
            var schoolClass = classes.FirstOrDefault(x => string.Equals(x.Name, className, StringComparison.OrdinalIgnoreCase)); var course = courses.FirstOrDefault(x => string.Equals(x.Name, subjectName, StringComparison.OrdinalIgnoreCase)); var teacher = teachers.FirstOrDefault(x => string.Equals(x.FullName, teacherName, StringComparison.OrdinalIgnoreCase)); var resource = resources.FirstOrDefault(x => string.Equals(x.Name, resourceName, StringComparison.OrdinalIgnoreCase));
            if (schoolClass is null && string.IsNullOrWhiteSpace(className)) schoolClass = new SchoolClass(Guid.NewGuid(), $"Sınıf referansı yok · {lesson.Id}", 0, "?", "ASC sınıf referansı eksik", false);
            if (schoolClass is null || course is null || teacher is null) continue;
            var weeklyHours = Number(lesson.PeriodsPerWeek, Math.Max(1, course.WeeklyHours)); var blockLength = Number(lesson.PeriodsPerCard, 1); var patterns = blockLength > 1 ? new[] { string.Join('+', Enumerable.Repeat(blockLength.ToString(CultureInfo.InvariantCulture), Math.Max(1, weeklyHours / blockLength))) } : new[] { "1" };
            var request = new LessonRequest(Guid.NewGuid(), schoolClass, course, teacher, resource, weeklyHours, patterns); requests.Add(request); requestByExternal[lesson.Id] = request; mapped++;
        }
        var protectedCards = new List<LessonAssignment>();
        var protectedCardIds = new Dictionary<Guid, long>();
        var mappedCards = 0;
        if (includeProtectedCards)
            foreach (var card in cards)
                if (requestByExternal.TryGetValue(card.Record.LessonId, out var request) && int.TryParse(card.Record.Period, out var originalPeriod))
                {
                    mappedCards++;
                    var period = originalPeriod;
                    var days = card.Record.Days;
                    if (overrideById.TryGetValue(card.Id, out var change))
                    {
                        if (change.IsRemoved) continue;
                        period = change.Period;
                        days = change.Days;
                    }
                    var dayIndex = days.IndexOf('1'); if (dayIndex < 0 || dayIndex > 4) continue;
                    var assignment = new LessonAssignment(Guid.NewGuid(), request.Class.Id, request.Course.Id, request.Teacher.Id, request.Resource?.Id, (DayOfWeek)((int)DayOfWeek.Monday + dayIndex), period, Number(lessons.First(x => x.Id == card.Record.LessonId).PeriodsPerCard, 1), true);
                    protectedCards.Add(assignment);
                    protectedCardIds[assignment.Id] = card.Id;
                }
        return new AscSolverInput(requests, protectedCards, protectedCardIds, requestByExternal, lessons.Count, cards.Count, mapped, mappedCards);
    }
    public async Task<IReadOnlyList<AscScheduleCard>> GetAscScheduleCardsAsync(CancellationToken t = default)
    {
        var input = await GetAscSolverInputAsync(true, t);
        var cards = await ReadAsync("SELECT Id,LessonId,Period,Days FROM AscCards WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1) ORDER BY Id", r => (Id: r.GetInt64(0), LessonId: r.GetString(1), Period: r.GetString(2), Days: r.GetString(3)), t);
        var overrides = await ReadAsync("SELECT CardId,Period,Days,IsRemoved FROM AscCardOverrides", r => new AscCardOverride(r.GetInt64(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3) == 1), t);
        var overrideById = overrides.ToDictionary(x => x.CardId);
        var teachers = await GetTeachersAsync(t); var resources = await GetResourcesAsync(t); var result = new List<AscScheduleCard>();
        foreach (var card in cards)
        {
            if (!input.RequestsByExternalLessonId.TryGetValue(card.LessonId, out var request)) continue;
            var period = int.TryParse(card.Period, out var parsedPeriod) ? parsedPeriod : 1; var days = card.Days; var removed = false; var manual = false;
            if (overrideById.TryGetValue(card.Id, out var change)) { period = change.Period; days = change.Days; removed = change.IsRemoved; manual = true; }
            var dayIndex = days.IndexOf('1'); if (dayIndex < 0 || dayIndex > 4) continue;
            var teacher = teachers.FirstOrDefault(x => x.Id == request.Teacher.Id); var resource = resources.FirstOrDefault(x => x.Id == request.Resource?.Id);
            var lesson = input.Requests.First(x => x.Id == request.Id);
            var blockLength = lesson.BlockPatterns.Select(x => int.TryParse(x.Split('+')[0], out var length) ? length : 1).DefaultIfEmpty(1).First();
            result.Add(new AscScheduleCard(card.Id, card.LessonId, request.Class.Name, request.Course.Name, request.Teacher.FullName, resource?.Name ?? "", teacher?.ColorCode ?? "", new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" }[dayIndex], period, blockLength, manual, removed));
        }
        return result;
    }
    public Task SaveAscCardOverrideAsync(AscCardOverride value, CancellationToken t = default) => WriteAsync("INSERT INTO AscCardOverrides (CardId,Period,Days,IsRemoved,UpdatedAt) VALUES ($id,$period,$days,$removed,$updated) ON CONFLICT(CardId) DO UPDATE SET Period=$period,Days=$days,IsRemoved=$removed,UpdatedAt=$updated", t, ("$id", value.CardId), ("$period", value.Period), ("$days", value.Days), ("$removed", value.IsRemoved ? 1 : 0), ("$updated", DateTimeOffset.Now.ToString("O")));
    public Task SaveTeacherAsync(Teacher value, CancellationToken t = default) => WriteAsync("INSERT INTO Teachers (Id,FullName,WeeklyMaximumHours,PreferredDayOff,Code,IsDemo,Department,AvailabilityNote,StaffStatus,SourceLabel,SourceUrl,SourceAccessDate,ColorCode) VALUES ($id,$name,$hours,$day,$code,$demo,$department,$note,$status,$label,$url,$date,$color) ON CONFLICT(Id) DO UPDATE SET FullName=$name,WeeklyMaximumHours=$hours,PreferredDayOff=$day,Code=$code,IsDemo=$demo,Department=$department,AvailabilityNote=$note,StaffStatus=$status,SourceLabel=$label,SourceUrl=$url,SourceAccessDate=$date,ColorCode=$color", t, ("$id", value.Id.ToString()), ("$name", value.FullName), ("$hours", value.WeeklyMaximumHours), ("$day", value.PreferredDayOff is null ? DBNull.Value : value.PreferredDayOff), ("$code", value.Code ?? (object)DBNull.Value), ("$demo", value.IsDemo ? 1 : 0), ("$department", value.Department ?? (object)DBNull.Value), ("$note", value.AvailabilityNote ?? (object)DBNull.Value), ("$status", value.StaffStatus), ("$label", value.SourceLabel ?? (object)DBNull.Value), ("$url", value.SourceUrl ?? (object)DBNull.Value), ("$date", value.SourceAccessDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value), ("$color", value.ColorCode ?? (object)DBNull.Value));
    public async Task ImportTeachersAsync(IReadOnlyList<Teacher> values, CancellationToken t = default) { var current = await GetTeachersAsync(t); foreach (var incoming in values) { var old = current.FirstOrDefault(x => string.Equals(x.FullName, incoming.FullName, StringComparison.OrdinalIgnoreCase)); var merged = old is null ? incoming : incoming with { Id = old.Id, WeeklyMaximumHours = old.WeeklyMaximumHours, PreferredDayOff = old.PreferredDayOff, IsDemo = old.IsDemo, AvailabilityNote = old.AvailabilityNote, StaffStatus = old.StaffStatus, ColorCode = old.ColorCode ?? incoming.ColorCode }; await SaveTeacherAsync(merged, t); } }
    public async Task CleanTeachersForAscAsync(IReadOnlyList<Teacher> xmlTeachers, CancellationToken t = default)
    {
        static string Key(string value) => new string(value.Replace("DEMO · ", "", StringComparison.OrdinalIgnoreCase).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        const string special = "MEHMETAKIFSONMEZ"; var current = await GetTeachersAsync(t); var xmlKeys = xmlTeachers.Select(x => Key(x.FullName)).ToHashSet(); var keep = new HashSet<Guid>(); var specialKept = false;
        foreach (var teacher in current)
        {
            var key = Key(teacher.FullName); if (xmlKeys.Contains(key)) { keep.Add(teacher.Id); continue; }
            if (key == special && !specialKept) { keep.Add(teacher.Id); specialKept = true; }
        }
        await using var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(t); await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(t);
        try
        {
            foreach (var teacher in current.Where(x => !keep.Contains(x.Id))) await ExecuteInTransactionAsync(connection, transaction, "DELETE FROM Teachers WHERE Id=$id", t, ("$id", teacher.Id.ToString()));
            var specialTeacher = current.FirstOrDefault(x => keep.Contains(x.Id) && Key(x.FullName) == special);
            if (xmlKeys.Contains(special)) { }
            else if (specialTeacher is null) await ExecuteInTransactionAsync(connection, transaction, "INSERT INTO Teachers (Id,FullName,WeeklyMaximumHours,Code,IsDemo,StaffStatus,SourceLabel) VALUES ($id,$name,30,$code,0,$status,$label)", t, ("$id", Guid.NewGuid().ToString()), ("$name", "Mehmet Akif Sönmez"), ("$code", "MAS"), ("$status", "Kadrolu"), ("$label", "Uygulama içi ekleme"));
            else await ExecuteInTransactionAsync(connection, transaction, "UPDATE Teachers SET FullName=$name,Code=COALESCE(NULLIF(Code,''),'MAS'),IsDemo=0,StaffStatus=$status,SourceLabel=$label,SourceUrl=NULL,SourceAccessDate=NULL WHERE Id=$id", t, ("$id", specialTeacher.Id.ToString()), ("$name", "Mehmet Akif Sönmez"), ("$status", "Kadrolu"), ("$label", "Uygulama içi ekleme"));
            await transaction.CommitAsync(t);
        }
        catch { await transaction.RollbackAsync(t); throw; }
    }
    public async Task CleanDemoDataAsync(CancellationToken t = default)
    {
        await using var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(t); await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(t);
        try
        {
            foreach (var sql in new[] { "DELETE FROM WorkplaceVocationalTrainingAssignments WHERE IsDemo=1", "DELETE FROM SchoolClasses WHERE IsDemo=1", "DELETE FROM Courses WHERE IsDemo=1", "DELETE FROM Resources WHERE IsDemo=1", "DELETE FROM Teachers WHERE IsDemo=1", "DELETE FROM CurriculumImportProfiles WHERE SourceLabel LIKE '%DEMO%' OR SourceLabel LIKE 'Demo%'", "DELETE FROM StudentProgramTransitions WHERE CohortName LIKE 'DEMO%'", "DELETE FROM ProgramTransitionRules WHERE SourceLabel LIKE '%DEMO%' OR SourceLabel LIKE 'Demo%'", "INSERT OR REPLACE INTO AppFlags (Key,Value) VALUES ('DemoSeedDisabled','1')" }) await ExecuteInTransactionAsync(connection, transaction, sql, t);
            await transaction.CommitAsync(t);
        }
        catch { await transaction.RollbackAsync(t); throw; }
    }
    public async Task ImportAscXmlAsync(AscXmlPreview preview, CancellationToken t = default)
    {
        var importId = Guid.NewGuid(); await using var c = new SqliteConnection(_connectionString); await c.OpenAsync(t); await using var tx = await c.BeginTransactionAsync(t);
        try { await ExecuteAsync(c, "INSERT INTO AscXmlImports VALUES ($id,$path,$name,$date,1)", t, ("$id", importId.ToString()), ("$path", preview.SourcePath), ("$name", preview.DisplayName), ("$date", DateTimeOffset.Now.ToString("O"))); foreach (var n in preview.Nodes) await ExecuteAsync(c, "INSERT INTO AscXmlNodes VALUES ($id,$section,$external,$name,$raw)", t, ("$id", importId.ToString()), ("$section", n.Section), ("$external", n.ExternalId), ("$name", n.Name), ("$raw", n.RawXml)); foreach (var x in preview.GroupRecords) await ExecuteAsync(c, "INSERT INTO AscGroups VALUES ($id,$external,$name,$class,$students,$entire,$division)", t, ("$id", importId.ToString()), ("$external", x.Id), ("$name", x.Name), ("$class", x.ClassId), ("$students", x.StudentIds), ("$entire", x.EntireClass ? 1 : 0), ("$division", x.DivisionTag)); foreach (var x in preview.LessonRecords) await ExecuteAsync(c, "INSERT INTO AscLessons VALUES ($id,$external,$classes,$subject,$teachers,$rooms,$groups,$ppc,$ppw,$days,$weeks,$terms)", t, ("$id", importId.ToString()), ("$external", x.Id), ("$classes", x.ClassIds), ("$subject", x.SubjectId), ("$teachers", x.TeacherIds), ("$rooms", x.ClassroomIds), ("$groups", x.GroupIds), ("$ppc", x.PeriodsPerCard), ("$ppw", x.PeriodsPerWeek), ("$days", x.DaysDefId), ("$weeks", x.WeeksDefId), ("$terms", x.TermsDefId)); foreach (var x in preview.CardRecords) await ExecuteAsync(c, "INSERT INTO AscCards (ImportId,LessonId,ClassroomIds,Period,Weeks,Terms,Days) VALUES ($id,$lesson,$rooms,$period,$weeks,$terms,$days)", t, ("$id", importId.ToString()), ("$lesson", x.LessonId), ("$rooms", x.ClassroomIds), ("$period", x.Period), ("$weeks", x.Weeks), ("$terms", x.Terms), ("$days", x.Days)); await tx.CommitAsync(t); }
        catch { await tx.RollbackAsync(t); throw; }
        await ImportTeachersAsync(AscXmlCoreMapping.Teachers(preview), t); foreach (var x in AscXmlCoreMapping.Classes(preview)) await SaveClassAsync(x, t); foreach (var x in AscXmlCoreMapping.Courses(preview)) await SaveCourseAsync(x, t); foreach (var x in AscXmlCoreMapping.Resources(preview)) await SaveResourceAsync(x, t);
    }
    #if false
    private async Task ExportAscXmlLegacyAsync(string path, CancellationToken t = default)
    {
        var teachers = await GetTeachersAsync(t); var classes = await GetClassesAsync(t); var courses = await GetCoursesAsync(t); var resources = await GetResourcesAsync(t); var groups = await ReadAsync("SELECT ExternalId,Name,ClassId,StudentIds,EntireClass,DivisionTag FROM AscGroups WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscGroupRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4) == 1, r.GetString(5)), t); var lessons = await ReadAsync("SELECT ExternalId,ClassIds,SubjectId,TeacherIds,ClassroomIds,GroupIds,PeriodsPerCard,PeriodsPerWeek,DaysDefId,WeeksDefId,TermsDefId FROM AscLessons WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscLessonRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10)), t); var cards = await ReadAsync("SELECT LessonId,ClassroomIds,Period,Weeks,Terms,Days FROM AscCards WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscCardRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5)), t); var periods = Enumerable.Range(1, 10).Select(i => new XElement("period", new XAttribute("name", $"{i}.Ders"), new XAttribute("short", i), new XAttribute("period", i))); var root = new XElement("timetable", new XAttribute("importtype", "database"), new XAttribute("displayname", "aSc Timetables 2012 XML"), new XElement("periods", periods), new XElement("breaks", new XElement("break", new XAttribute("name", "ÖĞLE ARASI 1"), new XAttribute("short", "ÖĞLE ARASI 1"), new XAttribute("break", 5), new XAttribute("starttime", "12:10"), new XAttribute("endtime", "13:00")), new XElement("break", new XAttribute("name", "ÖĞLE ARASI 2"), new XAttribute("short", "ÖĞLE ARASI 2"), new XElement("break", new XAttribute("name", "ÖĞLE ARASI 2"), new XAttribute("short", "ÖĞLE ARASI 2"), new XAttribute("break", 6), new XAttribute("starttime", "13:00"), new XAttribute("endtime", "13:50"))), new XElement("daysdefs", new[] { ("Pazartesi", "10000"), ("Salı", "01000"), ("Çarşamba", "00100"), ("Perşembe", "00010"), ("Cuma", "00001") }.Select((x, i) => new XElement("daysdef", new XAttribute("id", Guid.NewGuid().ToString("N")), new XAttribute("name", x.Item1), new XAttribute("short", x.Item1[..2]), new XAttribute("days", x.Item2)))), new XElement("subjects", courses.Select(x => new XElement("subject", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Abbreviation ?? x.Name[..Math.Min(3, x.Name.Length)])))), new XElement("teachers", teachers.Select(x => new XElement("teacher", new XAttribute("id", x.Id), new XAttribute("name", x.FullName), new XAttribute("short", x.Code ?? "")))), new XElement("classrooms", resources.Select(x => new XElement("classroom", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Name), new XAttribute("capacity", x.Capacity)))), new XElement("classes", classes.Select(x => new XElement("class", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Name)))), new XElement("groups", groups.Select(x => new XElement("group", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("classid", x.ClassId), new XAttribute("studentids", x.StudentIds), new XAttribute("entireclass", x.EntireClass ? 1 : 0), new XAttribute("divisiontag", x.DivisionTag)))), new XElement("lessons", lessons.Select(x => new XElement("lesson", new XAttribute("id", x.Id), new XAttribute("classids", x.ClassIds), new XAttribute("subjectid", x.SubjectId), new XAttribute("teacherids", x.TeacherIds), new XAttribute("classroomids", x.ClassroomIds), new XAttribute("groupids", x.GroupIds), new XAttribute("periodspercard", x.PeriodsPerCard), new XAttribute("periodsperweek", x.PeriodsPerWeek), new XAttribute("daysdefid", x.DaysDefId), new XAttribute("weeksdefid", x.WeeksDefId), new XAttribute("termsdefid", x.TermsDefId)))), new XElement("cards", cards.Select(x => new XElement("card", new XAttribute("lessonid", x.LessonId), new XAttribute("classroomids", x.ClassroomIds), new XAttribute("period", x.Period), new XAttribute("weeks", x.Weeks), new XAttribute("terms", x.Terms), new XAttribute("days", x.Days)))), new XElement("appmetadata", new XAttribute("source", "Ders Dağıtım Uygulaması"), new XAttribute("warning", "ASC dışı kadro, kilit ve manuel alanlar metadata ile ayrıca eşlenecektir."))); await using var fs = File.Create(path); var settings = new System.Xml.XmlWriterSettings { Async = true, Encoding = new System.Text.UTF8Encoding(false), Indent = true }; using var writer = System.Xml.XmlWriter.Create(fs, settings); root.Save(writer); await writer.FlushAsync();
    }
    #endif
    private async Task ExportAscXmlLegacyActiveAsync(string path, CancellationToken t = default)
    {
        var groups = await ReadAsync("SELECT ExternalId,Name,ClassId,StudentIds,EntireClass,DivisionTag FROM AscGroups WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscGroupRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4) == 1, r.GetString(5)), t);
        var lessons = await ReadAsync("SELECT ExternalId,ClassIds,SubjectId,TeacherIds,ClassroomIds,GroupIds,PeriodsPerCard,PeriodsPerWeek,DaysDefId,WeeksDefId,TermsDefId FROM AscLessons WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscLessonRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10)), t);
        var cards = await ReadAsync("SELECT LessonId,ClassroomIds,Period,Weeks,Terms,Days FROM AscCards WHERE ImportId=(SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1)", r => new AscCardRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5)), t);
        var teachers = await GetTeachersAsync(t); var classes = await GetClassesAsync(t); var courses = await GetCoursesAsync(t); var resources = await GetResourcesAsync(t);
        var root = new XElement("timetable", new XAttribute("importtype", "database"), new XAttribute("displayname", "aSc Timetables 2012 XML"), new XElement("periods", Enumerable.Range(1, 10).Select(i => new XElement("period", new XAttribute("name", $"{i}.Ders"), new XAttribute("short", i), new XAttribute("period", i)))), new XElement("breaks", new XElement("break", new XAttribute("name", "ÖĞLE ARASI 1"), new XAttribute("short", "ÖĞLE ARASI 1"), new XAttribute("break", 5), new XAttribute("starttime", "12:10"), new XAttribute("endtime", "13:00")), new XElement("break", new XAttribute("name", "ÖĞLE ARASI 2"), new XAttribute("short", "ÖĞLE ARASI 2"), new XAttribute("break", 6), new XAttribute("starttime", "13:00"), new XAttribute("endtime", "13:50"))), new XElement("subjects", courses.Select(x => new XElement("subject", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Abbreviation ?? "")))), new XElement("teachers", teachers.Select(x => new XElement("teacher", new XAttribute("id", x.Id), new XAttribute("name", x.FullName), new XAttribute("short", x.Code ?? "")))), new XElement("classrooms", resources.Select(x => new XElement("classroom", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Name), new XAttribute("capacity", x.Capacity)))), new XElement("classes", classes.Select(x => new XElement("class", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("short", x.Name)))), new XElement("groups", groups.Select(x => new XElement("group", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("classid", x.ClassId), new XAttribute("studentids", x.StudentIds), new XAttribute("entireclass", x.EntireClass ? 1 : 0), new XAttribute("divisiontag", x.DivisionTag)))), new XElement("lessons", lessons.Select(x => new XElement("lesson", new XAttribute("id", x.Id), new XAttribute("classids", x.ClassIds), new XAttribute("subjectid", x.SubjectId), new XAttribute("teacherids", x.TeacherIds), new XAttribute("classroomids", x.ClassroomIds), new XAttribute("groupids", x.GroupIds), new XAttribute("periodspercard", x.PeriodsPerCard), new XAttribute("periodsperweek", x.PeriodsPerWeek), new XAttribute("daysdefid", x.DaysDefId), new XAttribute("weeksdefid", x.WeeksDefId), new XAttribute("termsdefid", x.TermsDefId)))), new XElement("cards", cards.Select(x => new XElement("card", new XAttribute("lessonid", x.LessonId), new XAttribute("classroomids", x.ClassroomIds), new XAttribute("period", x.Period), new XAttribute("weeks", x.Weeks), new XAttribute("terms", x.Terms), new XAttribute("days", x.Days)))), new XElement("appmetadata", new XAttribute("source", "Ders Dağıtım Uygulaması"), new XAttribute("warning", "ASC dışı alanlar ayrıca eşlenmelidir.")));
        await using var fs = File.Create(path); var settings = new System.Xml.XmlWriterSettings { Async = true, Encoding = new System.Text.UTF8Encoding(false), Indent = true }; using var writer = System.Xml.XmlWriter.Create(fs, settings); root.Save(writer); await writer.FlushAsync();
    }
    public async Task ExportAscXmlAsync(string path, CancellationToken t = default)
    {
        var importId = (await ReadAsync("SELECT Id FROM AscXmlImports ORDER BY ImportedAt DESC LIMIT 1", r => r.GetString(0), t)).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(importId)) throw new InvalidOperationException("Dışa aktarılacak ASC kurum verisi bulunamadı.");
        var importFilter = $"'{importId.Replace("'", "''")}'";
        var rawNodes = await ReadAsync($"SELECT RawXml FROM AscXmlNodes WHERE ImportId={importFilter}", r => r.GetString(0), t);
        var groups = await ReadAsync($"SELECT ExternalId,Name,ClassId,StudentIds,EntireClass,DivisionTag FROM AscGroups WHERE ImportId={importFilter}", r => new AscGroupRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4) == 1, r.GetString(5)), t);
        var lessons = await ReadAsync($"SELECT ExternalId,ClassIds,SubjectId,TeacherIds,ClassroomIds,GroupIds,PeriodsPerCard,PeriodsPerWeek,DaysDefId,WeeksDefId,TermsDefId FROM AscLessons WHERE ImportId={importFilter}", r => new AscLessonRecord(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10)), t);
        var cards = await ReadAsync($"SELECT Id,LessonId,ClassroomIds,Period,Weeks,Terms,Days FROM AscCards WHERE ImportId={importFilter} ORDER BY Id", r => (Id: r.GetInt64(0), Record: new AscCardRecord(r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6))), t);
        var overrides = await ReadAsync("SELECT CardId,Period,Days,IsRemoved FROM AscCardOverrides", r => new AscCardOverride(r.GetInt64(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3) == 1), t);
        var overrideById = overrides.ToDictionary(x => x.CardId);
        var importedNodes = rawNodes.Select(XElement.Parse).ToArray();
        IEnumerable<XElement> Nodes(string localName) => importedNodes.Where(x => x.Name.LocalName == localName).Select(x => new XElement(x));
        IEnumerable<XElement> ExportCards()
        {
            foreach (var card in cards)
            {
                var record = card.Record;
                if (overrideById.TryGetValue(card.Id, out var change))
                {
                    if (change.IsRemoved) continue;
                    record = record with { Period = change.Period.ToString(CultureInfo.InvariantCulture), Days = change.Days };
                }
                yield return new XElement("card", new XAttribute("lessonid", record.LessonId), new XAttribute("classroomids", record.ClassroomIds), new XAttribute("period", record.Period), new XAttribute("weeks", record.Weeks), new XAttribute("terms", record.Terms), new XAttribute("days", record.Days));
            }
        }
        var root = new XElement("timetable",
            new XAttribute("importtype", "database"),
            new XAttribute("displayname", "aSc Timetables 2012 XML"),
            new XElement("periods", Nodes("period")),
            new XElement("breaks", Nodes("break")),
            new XElement("daysdefs", Nodes("daysdef")),
            new XElement("weeksdefs", Nodes("weeksdef")),
            new XElement("termsdefs", Nodes("termsdef")),
            new XElement("subjects", Nodes("subject")),
            new XElement("teachers", Nodes("teacher")),
            new XElement("buildings", Nodes("building")),
            new XElement("classrooms", Nodes("classroom")),
            new XElement("classes", Nodes("class")),
            new XElement("groups", groups.Select(x => new XElement("group", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("classid", x.ClassId), new XAttribute("studentids", x.StudentIds), new XAttribute("entireclass", x.EntireClass ? 1 : 0), new XAttribute("divisiontag", x.DivisionTag)))),
            new XElement("lessons", lessons.Select(x => new XElement("lesson", new XAttribute("id", x.Id), new XAttribute("classids", x.ClassIds), new XAttribute("subjectid", x.SubjectId), new XAttribute("teacherids", x.TeacherIds), new XAttribute("classroomids", x.ClassroomIds), new XAttribute("groupids", x.GroupIds), new XAttribute("periodspercard", x.PeriodsPerCard), new XAttribute("periodsperweek", x.PeriodsPerWeek), new XAttribute("daysdefid", x.DaysDefId), new XAttribute("weeksdefid", x.WeeksDefId), new XAttribute("termsdefid", x.TermsDefId)))),
            new XElement("cards", ExportCards()),
            new XElement("appmetadata", new XAttribute("source", "Ders Dağıtım Uygulaması"), new XAttribute("warning", "ASC external ID değerleri korunur; manuel kart değişiklikleri cards çıktısına uygulanır.")));
        await using var fs = File.Create(path); var settings = new System.Xml.XmlWriterSettings { Async = true, Encoding = new System.Text.UTF8Encoding(false), Indent = true }; using var writer = System.Xml.XmlWriter.Create(fs, settings); root.Save(writer); await writer.FlushAsync();
    }
    public Task SaveClassAsync(SchoolClass value, CancellationToken t = default) => WriteAsync("INSERT INTO SchoolClasses (Id,Name,Grade,Branch,Department,IsDemo,ProgramType) VALUES ($id,$name,$grade,$branch,$department,$demo,$program) ON CONFLICT(Id) DO UPDATE SET Name=$name,Grade=$grade,Branch=$branch,Department=$department,IsDemo=$demo,ProgramType=$program", t, ("$id", value.Id.ToString()), ("$name", value.Name), ("$grade", value.Grade), ("$branch", value.Branch), ("$department", value.Department ?? (object)DBNull.Value), ("$demo", value.IsDemo ? 1 : 0), ("$program", (int)value.ProgramType));
    public Task SaveCourseAsync(Course value, CancellationToken t = default) => WriteAsync("INSERT INTO Courses (Id,Name,WeeklyHours,BlockOptions,IsPractical,Abbreviation,SourceLabel,SourceVersion,IsDemo,CourseType,IsElective) VALUES ($id,$name,$hours,$blocks,$practical,$abbr,$source,$version,$demo,$type,$elective) ON CONFLICT(Id) DO UPDATE SET Name=$name,WeeklyHours=$hours,BlockOptions=$blocks,IsPractical=$practical,Abbreviation=$abbr,SourceLabel=$source,SourceVersion=$version,IsDemo=$demo,CourseType=$type,IsElective=$elective", t, ("$id", value.Id.ToString()), ("$name", value.Name), ("$hours", value.WeeklyHours), ("$blocks", string.Join(',', value.BlockOptions)), ("$practical", value.IsPractical ? 1 : 0), ("$abbr", value.Abbreviation ?? (object)DBNull.Value), ("$source", value.SourceLabel ?? (object)DBNull.Value), ("$version", value.SourceVersion ?? (object)DBNull.Value), ("$demo", value.IsDemo ? 1 : 0), ("$type", (int)value.Type), ("$elective", value.IsElective ? 1 : 0));
    public Task SaveResourceAsync(Resource value, CancellationToken t = default) => WriteAsync("INSERT INTO Resources (Id,Name,Capacity,ResourceType,IsDemo) VALUES ($id,$name,$capacity,$type,0) ON CONFLICT(Id) DO UPDATE SET Name=$name,Capacity=$capacity,ResourceType=$type,IsDemo=0", t, ("$id", value.Id.ToString()), ("$name", value.Name), ("$capacity", value.Capacity), ("$type", (int)value.Type));
    public Task DeleteAsync(string entity, Guid id, CancellationToken t = default) => WriteAsync($"DELETE FROM {entity} WHERE Id=$id", t, ("$id", id.ToString()));

    private async Task EnsureLegacyColumnsAsync(SqliteConnection connection, CancellationToken token)
    {
        foreach (var sql in new[] { "ALTER TABLE SchoolClasses ADD COLUMN Department TEXT NULL", "ALTER TABLE SchoolClasses ADD COLUMN IsDemo INTEGER NOT NULL DEFAULT 1", "ALTER TABLE SchoolClasses ADD COLUMN ProgramType INTEGER NOT NULL DEFAULT 0", "ALTER TABLE Teachers ADD COLUMN Code TEXT NULL", "ALTER TABLE Teachers ADD COLUMN IsDemo INTEGER NOT NULL DEFAULT 1", "ALTER TABLE Teachers ADD COLUMN Department TEXT NULL", "ALTER TABLE Teachers ADD COLUMN AvailabilityNote TEXT NULL", "ALTER TABLE Teachers ADD COLUMN StaffStatus TEXT NOT NULL DEFAULT 'Kadrolu'", "ALTER TABLE Teachers ADD COLUMN SourceLabel TEXT NULL", "ALTER TABLE Teachers ADD COLUMN SourceUrl TEXT NULL", "ALTER TABLE Teachers ADD COLUMN SourceAccessDate TEXT NULL", "ALTER TABLE Courses ADD COLUMN Abbreviation TEXT NULL", "ALTER TABLE Courses ADD COLUMN SourceLabel TEXT NULL", "ALTER TABLE Courses ADD COLUMN SourceVersion TEXT NULL", "ALTER TABLE Courses ADD COLUMN IsDemo INTEGER NOT NULL DEFAULT 1", "ALTER TABLE Courses ADD COLUMN CourseType INTEGER NOT NULL DEFAULT 0", "ALTER TABLE Courses ADD COLUMN IsElective INTEGER NOT NULL DEFAULT 0" })
        {
            try { await using var command = connection.CreateCommand(); command.CommandText = sql; await command.ExecuteNonQueryAsync(token); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)) { }
        }
    }
    private static async Task RemoveProvenancePrefixesAsync(SqliteConnection connection, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "UPDATE Courses SET Name=substr(Name,7) WHERE Name LIKE 'ASC ' || char(183) || ' %'; UPDATE Resources SET Name=substr(Name,7) WHERE Name LIKE 'ASC ' || char(183) || ' %';"; await command.ExecuteNonQueryAsync(token);
    }
    private static async Task EnsureTeacherColorColumnAsync(SqliteConnection connection, CancellationToken token)
    {
        try { await using var command = connection.CreateCommand(); command.CommandText = "ALTER TABLE Teachers ADD COLUMN ColorCode TEXT NULL"; await command.ExecuteNonQueryAsync(token); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
    }

    private async Task SeedDemoDataAsync(SqliteConnection connection, CancellationToken token)
    {
        await using (var flag = connection.CreateCommand()) { flag.CommandText = "SELECT Value FROM AppFlags WHERE Key='DemoSeedDisabled'"; if (Convert.ToString(await flag.ExecuteScalarAsync(token)) == "1") return; }
        await using var count = connection.CreateCommand(); count.CommandText = "SELECT COUNT(*) FROM SchoolClasses";
        if (Convert.ToInt32(await count.ExecuteScalarAsync(token)) > 0) { await SeedAdditionalDemoDataAsync(connection, token); return; }
        var departments = new[] { "Bilişim Teknolojileri", "Elektrik-Elektronik Teknolojisi", "Biyomedikal Cihaz Teknolojileri" };
        var classes = new[] { (9,"D"),(10,"C"),(10,"D"),(11,"D"),(11,"E"),(11,"F"),(11,"G"),(12,"C"),(12,"D") };
        foreach (var (grade, branch) in classes) await ExecuteAsync(connection, "INSERT INTO SchoolClasses VALUES ($id,$name,$grade,$branch,$department,1,$program)", token, ("$id", Guid.NewGuid().ToString()), ("$name", $"{grade}/{branch}"), ("$grade", grade), ("$branch", branch), ("$department", departments[0]), ("$program", grade >= 11 && branch == "G" ? 1 : 0));
        var teachers = new[] { ("Ayşe Demir","AD"),("Mehmet Akif Sönmez","MAS"),("Selin Kaya","SK"),("Deniz Yılmaz","DY"),("Burak Çetin","BÇ") };
        foreach (var (name, code) in teachers) await ExecuteAsync(connection, "INSERT INTO Teachers (Id,FullName,WeeklyMaximumHours,PreferredDayOff,Code,IsDemo) VALUES ($id,$name,30,NULL,$code,1)", token, ("$id", Guid.NewGuid().ToString()), ("$name", $"DEMO · {name}"), ("$code", code));
        var courses = new[] { ("Programlama Temelleri","PROG",5,"5,3+2",true),("Web Tasarımı","WEB",4,"4,2+2",true),("Grafik ve Animasyon","GRAFİK",4,"4,2+2",true),("Mobil Uygulamalar","MOB",20,"5,3+2",true),("Seçmeli Dijital Tasarım","SDT",2,"2,2",true),("Staj","STJ",4,"4",true),("Bilişim Üzümre","BÜ",1,"1",false) };
        foreach (var (name, abbreviation, hours, blocks, practical) in courses) await ExecuteAsync(connection, "INSERT INTO Courses VALUES ($id,$name,$hours,$blocks,$practical,$abbreviation,'Demo - MEB kaynağı doğrulanmadı','',1)", token, ("$id", Guid.NewGuid().ToString()), ("$name", $"DEMO · {name}"), ("$hours", hours), ("$blocks", blocks), ("$practical", practical ? 1 : 0), ("$abbreviation", abbreviation));
        await ExecuteAsync(connection, "INSERT INTO Resources VALUES ($id,$name,24,2,1)", token, ("$id", Guid.NewGuid().ToString()), ("$name", "Bilişim Laboratuvarı 3 · DEMO"));
        await ExecuteAsync(connection, "INSERT INTO CurriculumImportProfiles VALUES ($id,$department,$branch,$grade,$program,$year,$source)", token, ("$id", Guid.NewGuid().ToString()), ("$department", departments[0]), ("$branch", "Bilişim Teknolojileri"), ("$grade", 11), ("$program", 0), ("$year", "2025/2026"), ("$source", "MEB çerçeve program içe aktarma profili - kaynak seçimi bekleniyor"));
        await ExecuteAsync(connection, "INSERT INTO StudentProgramTransitions VALUES ($id,$cohort,$source,$target,$grade,$year,$status,$curriculum,$missing,$rule,$approval,$system)", token, ("$id", Guid.NewGuid().ToString()), ("$cohort", "DEMO · 10/C Bilişim Teknolojileri"), ("$source", 0), ("$target", 1), ("$grade", 11), ("$year", "2025/2026"), ("$status", 1), ("$curriculum", "Seçilecek resmî sürüm"), ("$missing", ""), ("$rule", "PROGRAM-GECIS-UYGUNLUK"), ("$approval", "Yönetim/e-Okul onayı bekleniyor"), ("$system", "e-Okul veya okul yönetimi"));
        await ExecuteAsync(connection, "INSERT INTO ProgramTransitionRules VALUES ($id,$code,$name,$description,1,$json,$source,$version)", token, ("$id", Guid.NewGuid().ToString()), ("$code", "PROGRAM-GECIS-UYGUNLUK"), ("$name", "Program türü geçiş uygunluğu"), ("$description", "AMP/ATP geçiş koşulları mevzuat ve okul onayıyla düzenlenir; sabit kodlanmaz."), ("$json", "{\"minimumAverage\":70,\"requiredApproval\":true}"), ("$source", "MEB mevzuatı / okul yönetimi"), ("$version", "Güncel sürüm seçilecek"));
        await ExecuteAsync(connection, "INSERT INTO WorkplaceVocationalTrainingAssignments VALUES ($id,$teacher,$class,$department,$branch,$group,4,$note,$year,1,1)", token, ("$id", Guid.NewGuid().ToString()), ("$teacher", ""), ("$class", "12/C"), ("$department", departments[0]), ("$branch", "Bilişim Teknolojileri"), ("$group", "DEMO · İşletme grubu A"), ("$note", "DEMO · İdari karar notu girilecek"), ("$year", "2025/2026"));
    }

    private static async Task SeedAdditionalDemoDataAsync(SqliteConnection connection, CancellationToken token)
    {
        var departments = new[] { "Elektrik-Elektronik Teknolojisi", "Biyomedikal Cihaz Teknolojileri" };
        foreach (var department in departments)
        {
            await using var check = connection.CreateCommand(); check.CommandText = "SELECT COUNT(*) FROM SchoolClasses WHERE Department=$department"; check.Parameters.AddWithValue("$department", department);
            if (Convert.ToInt32(await check.ExecuteScalarAsync(token)) > 0) continue;
            var prefix = department.StartsWith("Elektrik") ? "EE" : "BM";
            foreach (var (grade, branch) in new[] { (11, prefix + "A"), (12, prefix + "B") }) await ExecuteAsync(connection, "INSERT INTO SchoolClasses VALUES ($id,$name,$grade,$branch,$department,1,0)", token, ("$id", Guid.NewGuid().ToString()), ("$name", $"{grade}/{branch}"), ("$grade", grade), ("$branch", branch), ("$department", department));
            var courses = department.StartsWith("Elektrik") ? new[] { ("Elektrik Tesisatları","ELT",4,"4,2+2",true), ("Elektronik Uygulamaları","E-UYG",4,"4,2+2",true), ("Kumanda Sistemleri","KUM",3,"3,3",true) } : new[] { ("Biyomedikal Temelleri","BİY",4,"4,2+2",false), ("Tıbbi Cihaz Uygulamaları","TCU",5,"5,3+2",true), ("Ölçme ve Kalibrasyon","ÖLK",3,"3,3",true) };
            foreach (var (name, abbreviation, hours, blocks, practical) in courses) await ExecuteAsync(connection, "INSERT INTO Courses VALUES ($id,$name,$hours,$blocks,$practical,$abbreviation,'Demo - MEB kaynağı doğrulanmadı','',1)", token, ("$id", Guid.NewGuid().ToString()), ("$name", $"DEMO · {name}"), ("$hours", hours), ("$blocks", blocks), ("$practical", practical ? 1 : 0), ("$abbreviation", abbreviation));
            var resourceName = department.StartsWith("Elektrik") ? "Elektrik-Elektronik Atölyesi · DEMO" : "Biyomedikal Cihazlar Laboratuvarı · DEMO";
            await ExecuteAsync(connection, "INSERT INTO Resources VALUES ($id,$name,16,1,1)", token, ("$id", Guid.NewGuid().ToString()), ("$name", resourceName));
        }
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken token, params (string Name, object Value)[] values) { await using var command = connection.CreateCommand(); command.CommandText = sql; foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(token); }
    private static async Task ExecuteInTransactionAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken token, params (string Name, object Value)[] values) { await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(token); }
    private async Task WriteAsync(string sql, CancellationToken token, params (string Name, object Value)[] values) { await using var c = new SqliteConnection(_connectionString); await c.OpenAsync(token); await using var command = c.CreateCommand(); command.CommandText = sql; foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(token); }
    private async Task<IReadOnlyList<T>> ReadAsync<T>(string sql, Func<SqliteDataReader, T> map, CancellationToken token) { var list = new List<T>(); await using var c = new SqliteConnection(_connectionString); await c.OpenAsync(token); await using var x = c.CreateCommand(); x.CommandText = sql; await using var r = await x.ExecuteReaderAsync(token); while (await r.ReadAsync(token)) list.Add(map(r)); return list; }
}
