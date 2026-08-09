using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class LaboratoryScheduleWindow : Window
{
    private const string ClassMode = "Sınıf";
    private const string ResourceMode = "Kaynak/Lab";
    private const string AllClasses = "Tüm sınıflar";
    private const string AllResources = "Tüm kaynaklar";
    private IReadOnlyList<LessonAssignment> _assignments = Array.Empty<LessonAssignment>();
    private IReadOnlyList<LessonRequest> _requests = Array.Empty<LessonRequest>();
    private IReadOnlyList<Teacher> _teachers = Array.Empty<Teacher>();
    private IReadOnlyList<SchoolClass> _classes = Array.Empty<SchoolClass>();
    private IReadOnlyList<ClassTeacherAssignment> _classTeachers = Array.Empty<ClassTeacherAssignment>();
    private bool _ready;

    private static readonly DayOfWeek[] WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    private static readonly TimeColumn[] TimeColumns =
    [
        new(1, "1.Ders", "9:00 - 9:40", false),
        new(2, "2.Ders", "9:50 - 10:30", false),
        new(3, "3.Ders", "10:40 - 11:20", false),
        new(4, "4.Ders", "11:30 - 12:10", false),
        new(0, "OGLE ARASI 1", "12:10 - 13:00", true),
        new(5, "5.Ders", "13:00 - 13:40", false),
        new(0, "OGLE ARASI 2", "13:00 - 13:50", true),
        new(6, "6.Ders", "13:50 - 14:30", false),
        new(7, "7.Ders", "14:40 - 15:20", false),
        new(8, "8.Ders", "15:30 - 16:10", false),
        new(9, "9.Ders", "16:20 - 17:00", false),
        new(10, "10.Ders", "17:10 - 17:50", false)
    ];

    public LaboratoryScheduleWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _classes = await App.Dashboard.Repository.GetClassesAsync();
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();

        if (DraftWorkspace.Current is not null && DraftWorkspace.Requests.Count > 0)
        {
            _assignments = DraftWorkspace.Current.Assignments;
            _requests = DraftWorkspace.Requests;
            _teachers = DraftWorkspace.Teachers;
            StatusText.Text = "Taslak cizelge verisi kullaniliyor.";
        }
        else
        {
            var input = await App.Dashboard.Repository.GetAscSolverInputAsync(true);
            _assignments = input.ProtectedCards;
            _requests = input.Requests;
            _teachers = await App.Dashboard.Repository.GetTeachersAsync();
            StatusText.Text = "Taslak yok; mevcut ASC kartlari kullaniliyor.";
        }

        ClassTeacherCombo.ItemsSource = _teachers.OrderBy(x => x.FullName).Select(x => new TeacherItem(x.Id, x.FullName)).ToArray();
        ClassTeacherCombo.DisplayMemberPath = nameof(TeacherItem.Name);
        ClassTeacherCombo.SelectedValuePath = nameof(TeacherItem.Id);

        ModePicker.ItemsSource = new[] { ClassMode, ResourceMode };
        ModePicker.SelectedIndex = 0;
        _ready = true;
        FillEntities();
        ApplyFilter();
    }

    private void FillEntities()
    {
        if (ModePicker.SelectedItem?.ToString() == ResourceMode)
        {
            EntityPicker.ItemsSource = new[] { AllResources }
                .Concat(_requests.Select(x => x.Resource?.Name).Where(x => !string.IsNullOrWhiteSpace(x))!.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                .ToArray();
        }
        else
        {
            EntityPicker.ItemsSource = new[] { AllClasses }
                .Concat(_requests.Select(x => x.Class.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                .ToArray();
        }

        EntityPicker.SelectedIndex = 0;
        SyncClassTeacherPanel();
    }

    private void ApplyFilter()
    {
        if (!_ready) return;

        var selected = EntityPicker.SelectedItem?.ToString();
        var mode = ModePicker.SelectedItem?.ToString();
        SyncClassTeacherPanel();

        var rows = _assignments
            .Select(x => (Assignment: x, Request: RequestFor(x)))
            .Where(x => x.Request is not null)
            .Where(x =>
                mode == ResourceMode
                    ? selected == AllResources || string.Equals(x.Request!.Resource?.Name, selected, StringComparison.OrdinalIgnoreCase)
                    : selected == AllClasses || string.Equals(x.Request!.Class.Name, selected, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => DayOrder(x.Assignment.Day))
            .ThenBy(x => x.Assignment.LessonNumber)
            .ThenBy(x => x.Request!.Class.Name)
            .Select(x => new Row(
                DayName(x.Assignment.Day),
                x.Assignment.LessonNumber,
                x.Request!.Class.Name,
                x.Request.Course.Name,
                TeacherNameFor(x.Assignment),
                x.Request.Resource?.Name ?? "-",
                x.Assignment.IsManual ? "Manuel" : "ASC/Taslak"))
            .ToArray();

        ScheduleGrid.ItemsSource = rows;
        StatusText.Text = $"{rows.Length} ders satiri gosteriliyor. Gorunum: {mode} - Secim: {selected}";
    }

    private void SyncClassTeacherPanel()
    {
        var selected = EntityPicker.SelectedItem?.ToString();
        var specificClass = ModePicker.SelectedItem?.ToString() == ClassMode && !string.IsNullOrWhiteSpace(selected) && selected != AllClasses;
        ClassTeacherPanel.Visibility = specificClass ? Visibility.Visible : Visibility.Collapsed;
        if (!specificClass)
        {
            ClassTeacherCombo.SelectedIndex = -1;
            return;
        }

        var schoolClass = _classes.FirstOrDefault(x => string.Equals(x.Name, selected, StringComparison.OrdinalIgnoreCase));
        var assigned = schoolClass is null ? null : _classTeachers.FirstOrDefault(x => x.ClassId == schoolClass.Id);
        ClassTeacherCombo.SelectedValue = assigned?.TeacherId ?? Guid.Empty;
    }

    private LessonRequest? RequestFor(LessonAssignment assignment) =>
        _requests.FirstOrDefault(x => x.Class.Id == assignment.ClassId && x.Course.Id == assignment.CourseId && x.Teacher.Id == assignment.TeacherId)
        ?? _requests.FirstOrDefault(x => x.Class.Id == assignment.ClassId && x.Course.Id == assignment.CourseId);

    private string TeacherNameFor(LessonAssignment assignment) =>
        _teachers.FirstOrDefault(x => x.Id == assignment.TeacherId)?.FullName
        ?? RequestFor(assignment)?.Teacher.FullName
        ?? "?";

    private void ModePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        FillEntities();
        ApplyFilter();
    }

    private void EntityPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private async void AssignClassTeacher_Click(object sender, RoutedEventArgs e)
    {
        var schoolClass = SelectedSchoolClass();
        if (schoolClass is null || ClassTeacherCombo.SelectedValue is not Guid teacherId)
        {
            StatusText.Text = "Sinif ogretmeni atamak icin tek bir sinif ve ogretmen secin.";
            return;
        }

        await App.Dashboard.Repository.SaveClassTeacherAssignmentAsync(new ClassTeacherAssignment(schoolClass.Id, teacherId, "2025/2026"));
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();
        SyncClassTeacherPanel();
        StatusText.Text = $"{schoolClass.Name} sinif ogretmeni kaydedildi.";
    }

    private async void RemoveClassTeacher_Click(object sender, RoutedEventArgs e)
    {
        var schoolClass = SelectedSchoolClass();
        if (schoolClass is null)
        {
            StatusText.Text = "Sinif ogretmeni atamasini kaldirmak icin tek bir sinif secin.";
            return;
        }

        await App.Dashboard.Repository.DeleteClassTeacherAssignmentAsync(schoolClass.Id);
        _classTeachers = await App.Dashboard.Repository.GetClassTeacherAssignmentsAsync();
        SyncClassTeacherPanel();
        StatusText.Text = $"{schoolClass.Name} sinif ogretmeni atamasi kaldirildi.";
    }

    private SchoolClass? SelectedSchoolClass() =>
        _classes.FirstOrDefault(x => string.Equals(x.Name, EntityPicker.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase));

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var rows = (ScheduleGrid.ItemsSource as IEnumerable<Row>)?.ToArray() ?? Array.Empty<Row>();
        if (rows.Length == 0) { StatusText.Text = "Aktarilacak satir yok."; return; }

        var dialog = new SaveFileDialog { Filter = "Excel uyumlu CSV|*.csv", FileName = SafeFileName($"{ModePicker.SelectedItem}-{EntityPicker.SelectedItem}-program.csv") };
        if (dialog.ShowDialog() != true) return;

        var lines = new List<string> { "Gun;Ders saati;Sinif;Ders;Ogretmen;Kaynak;Durum" };
        lines.AddRange(rows.Select(x => $"{x.DayName};{x.LessonNumber};{x.ClassName};{x.CourseName};{x.TeacherName};{x.ResourceName};{x.Status}"));
        File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(true));
        StatusText.Text = "CSV disa aktarildi.";
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var plans = CurrentPlans().ToArray();
            if (plans.Length == 0)
            {
                StatusText.Text = "Yazdirilacak plan yok.";
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;
            printDialog.PrintDocument(BuildPrintDocument(plans).DocumentPaginator, $"{ModePicker.SelectedItem} haftalik program");
            StatusText.Text = "aSc tarzi yatay plan PDF/yaziciya gonderildi. PDF printer secerek dosya kaydedebilirsiniz.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"PDF/yazdirma hatasi: {ex.Message}";
        }
    }

    private void ExportDoorLists_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "HTML dosyasi|*.html", FileName = SafeFileName($"{ModePicker.SelectedItem}-{EntityPicker.SelectedItem}-asc-plan.html") };
        if (dialog.ShowDialog() != true) return;

        File.WriteAllText(dialog.FileName, BuildDoorListsHtml(), new UTF8Encoding(false));
        StatusText.Text = "aSc tarzi yatay haftalik plan HTML olarak hazirlandi. Tarayicidan yazdirip PDF alabilirsiniz.";
    }

    private string BuildDoorListsHtml()
    {
        var plans = CurrentPlans().ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Haftalik Planlar</title>");
        sb.AppendLine("<style>@page{size:A4 landscape;margin:10mm}body{font-family:Arial,'Segoe UI',sans-serif;margin:0;color:#000}.page{page-break-after:always;padding:0 4px}.title{text-align:center;font-size:40px;line-height:1;margin:4px 0 0;font-weight:400}.school{font-size:15px;margin:0 0 3px}table{width:100%;border-collapse:collapse;table-layout:fixed;border:1.5px solid #000}.day{width:82px;font-size:40px;text-align:center;vertical-align:middle}.head{height:52px;text-align:center;vertical-align:middle;font-size:20px;font-weight:400}.head .time{font-size:10px;margin-top:8px}.cell{height:92px;vertical-align:top;position:relative;padding:5px 7px}.course{position:absolute;left:7px;top:5px;font-size:12px}.teacher{position:absolute;right:7px;top:5px;font-size:12px}.main{height:100%;display:flex;align-items:center;justify-content:center;text-align:center;font-size:31px;line-height:1.05}.break{font-size:30px;text-align:center;vertical-align:middle;writing-mode:vertical-rl;transform:rotate(180deg);letter-spacing:1px}.foot{display:flex;justify-content:space-between;font-size:13px;margin-top:3px}td,th{border:1px solid #000}.empty{height:92px}</style>");
        sb.AppendLine("</head><body>");

        foreach (var plan in plans)
        {
            sb.AppendLine("<section class=\"page\">");
            sb.AppendLine($"<h1 class=\"title\">{Html(plan.Title)}</h1>");
            sb.AppendLine("<div class=\"school\">TOKAT - MERKEZ / OZEL TOKAT DINAMIK MESLEKI VE TEKNIK ANADOLU LISESI</div>");
            sb.AppendLine("<table><thead><tr><th class=\"day\"></th>");
            foreach (var column in TimeColumns)
            {
                sb.AppendLine($"<th class=\"head\">{Html(column.Title)}<div class=\"time\">{Html(column.Time)}</div></th>");
            }
            sb.AppendLine("</tr></thead><tbody>");
            foreach (var day in WorkDays)
            {
                sb.AppendLine($"<tr><td class=\"day\">{Html(ShortDayName(day))}</td>");
                AppendHtmlDayCells(sb, plan, day);
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine($"<div class=\"foot\"><span>Ders Plani Olusturuldu: {DateTime.Today:dd.MM.yyyy}</span><span>Ders2z</span></div>");
            sb.AppendLine("</section>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private IEnumerable<PlanSheet> CurrentPlans()
    {
        var mode = ModePicker.SelectedItem?.ToString();
        var selected = EntityPicker.SelectedItem?.ToString();
        var rows = _assignments
            .Select(x => new PlanEntry(x, RequestFor(x)))
            .Where(x => x.Request is not null)
            .ToArray();

        if (mode == ResourceMode)
        {
            var groups = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Request!.Resource?.Name))
                .Where(x => selected == AllResources || string.Equals(x.Request!.Resource?.Name, selected, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Request!.Resource!.Name, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key);

            foreach (var group in groups)
            {
                yield return new PlanSheet(group.Key.ToUpperInvariant(), true, group.ToArray());
            }
        }
        else
        {
            var groups = rows
                .Where(x => selected == AllClasses || string.Equals(x.Request!.Class.Name, selected, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Request!.Class.Name, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key);

            foreach (var group in groups)
            {
                var schoolClass = _classes.FirstOrDefault(x => string.Equals(x.Name, group.Key, StringComparison.OrdinalIgnoreCase));
                var advisor = AdvisorName(schoolClass?.Id);
                var title = advisor == "Atanmadi" ? group.Key.ToUpperInvariant() : $"{group.Key.ToUpperInvariant()} - {advisor}";
                yield return new PlanSheet(title, false, group.ToArray());
            }
        }
    }

    private static void AppendHtmlDayCells(StringBuilder sb, PlanSheet plan, DayOfWeek day)
    {
        var hour = 1;
        while (hour <= 10)
        {
            if (hour == 5)
            {
                sb.AppendLine("<td class=\"break\">OGLE ARASI 1</td>");
            }

            if (hour == 6)
            {
                sb.AppendLine("<td class=\"break\">OGLE ARASI 2</td>");
            }

            var entry = plan.Entries
                .Where(x => x.Assignment.Day == day && x.Assignment.LessonNumber == hour)
                .OrderByDescending(x => x.Assignment.BlockLength)
                .FirstOrDefault();

            if (entry is null || entry.Request is null)
            {
                sb.AppendLine("<td class=\"empty\"></td>");
                hour++;
                continue;
            }

            var span = Math.Max(1, Math.Min(entry.Assignment.BlockLength, 11 - hour));
            var colspan = span > 1 ? $" colspan=\"{span}\"" : "";
            sb.AppendLine($"<td class=\"cell\"{colspan}>{HtmlCellContent(plan, entry)}</td>");
            hour += span;
        }
    }

    private static string HtmlCellContent(PlanSheet plan, PlanEntry entry)
    {
        var course = Html(ShortCourseName(entry.Request!.Course.Name));
        var teacher = Html(TeacherInitials(entry.Request.Teacher.FullName));
        var main = Html(plan.IsResourcePlan ? entry.Request.Class.Name : entry.Request.Resource?.Name ?? "");
        return $"<div class=\"course\">{course}</div><div class=\"teacher\">{teacher}</div><div class=\"main\">{main}</div>";
    }

    private FixedDocument BuildPrintDocument(IReadOnlyList<PlanSheet> plans)
    {
        var document = new FixedDocument();
        foreach (var plan in plans)
        {
            var page = new FixedPage { Width = 1122, Height = 794, Background = Brushes.White };
            var root = BuildPrintPage(plan);
            FixedPage.SetLeft(root, 24);
            FixedPage.SetTop(root, 22);
            page.Children.Add(root);
            var content = new PageContent();
            ((System.Windows.Markup.IAddChild)content).AddChild(page);
            document.Pages.Add(content);
        }

        return document;
    }

    private FrameworkElement BuildPrintPage(PlanSheet plan)
    {
        var root = new Grid { Width = 1074, Height = 744 };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(610) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

        root.Children.Add(new TextBlock { Text = plan.Title, FontSize = 46, FontWeight = FontWeights.Normal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });

        var school = new TextBlock { Text = "TOKAT - MERKEZ / OZEL TOKAT DINAMIK MESLEKI VE TEKNIK ANADOLU LISESI", FontSize = 15, VerticalAlignment = VerticalAlignment.Bottom };
        Grid.SetRow(school, 1);
        root.Children.Add(school);

        var table = BuildPrintTable(plan);
        Grid.SetRow(table, 2);
        root.Children.Add(table);

        var footer = new DockPanel { LastChildFill = false };
        footer.Children.Add(new TextBlock { Text = $"Ders Plani Olusturuldu: {DateTime.Today:dd.MM.yyyy}", FontSize = 14 });
        var right = new TextBlock { Text = "Ders2z", FontSize = 14 };
        DockPanel.SetDock(right, Dock.Right);
        footer.Children.Add(right);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private Grid BuildPrintTable(PlanSheet plan)
    {
        var grid = new Grid { Width = 1074, Height = 610 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(106) });
        foreach (var column in TimeColumns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
        foreach (var _ in WorkDays)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });
        }

        AddPrintBorder(grid, "", 0, 0, 1, 1, 12);
        for (var i = 0; i < TimeColumns.Length; i++)
        {
            AddPrintBorder(grid, $"{TimeColumns[i].Title}\n{TimeColumns[i].Time}", 0, i + 1, 1, 1, TimeColumns[i].IsBreak ? 11 : 22);
        }

        for (var dayIndex = 0; dayIndex < WorkDays.Length; dayIndex++)
        {
            var row = dayIndex + 1;
            AddPrintBorder(grid, ShortDayName(WorkDays[dayIndex]), row, 0, 1, 1, 44);
            AddPrintDayCells(grid, plan, WorkDays[dayIndex], row);
        }

        return grid;
    }

    private void AddPrintDayCells(Grid grid, PlanSheet plan, DayOfWeek day, int row)
    {
        var hour = 1;
        var column = 1;
        while (hour <= 10)
        {
            if (hour == 5)
            {
                AddPrintBorder(grid, "OGLE ARASI 1", row, column++, 1, 1, 28, rotate: true);
            }

            if (hour == 6)
            {
                AddPrintBorder(grid, "OGLE ARASI 2", row, column++, 1, 1, 28, rotate: true);
            }

            var entry = plan.Entries
                .Where(x => x.Assignment.Day == day && x.Assignment.LessonNumber == hour)
                .OrderByDescending(x => x.Assignment.BlockLength)
                .FirstOrDefault();

            if (entry is null || entry.Request is null)
            {
                AddPrintBorder(grid, "", row, column, 1, 1, 12);
                hour++;
                column++;
                continue;
            }

            var span = Math.Max(1, Math.Min(entry.Assignment.BlockLength, 11 - hour));
            AddPrintLessonCell(grid, plan, entry, row, column, span);
            hour += span;
            column += span;
        }
    }

    private static void AddPrintLessonCell(Grid grid, PlanSheet plan, PlanEntry entry, int row, int column, int columnSpan)
    {
        var border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.7), Padding = new Thickness(6), Background = Brushes.White };
        var cell = new Grid();
        cell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        cell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new DockPanel { LastChildFill = false };
        top.Children.Add(new TextBlock { Text = ShortCourseName(entry.Request!.Course.Name), FontSize = 12 });
        var teacher = new TextBlock { Text = TeacherInitials(entry.Request.Teacher.FullName), FontSize = 12 };
        DockPanel.SetDock(teacher, Dock.Right);
        top.Children.Add(teacher);
        cell.Children.Add(top);

        var main = new TextBlock
        {
            Text = plan.IsResourcePlan ? entry.Request.Class.Name : entry.Request.Resource?.Name ?? "",
            FontSize = columnSpan == 1 ? 30 : 34,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(main, 1);
        cell.Children.Add(main);
        border.Child = cell;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Grid.SetColumnSpan(border, columnSpan);
        grid.Children.Add(border);
    }

    private static void AddPrintBorder(Grid grid, string text, int row, int column, int rowSpan, int columnSpan, double fontSize, bool rotate = false)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        if (rotate)
        {
            block.LayoutTransform = new RotateTransform(-90);
        }

        var border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.8), Child = block };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Grid.SetRowSpan(border, rowSpan);
        Grid.SetColumnSpan(border, columnSpan);
        grid.Children.Add(border);
    }

    private string AdvisorName(Guid? classId)
    {
        if (classId is null) return "Atanmadi";
        var assignment = _classTeachers.FirstOrDefault(x => x.ClassId == classId.Value);
        return assignment is null ? "Atanmadi" : _teachers.FirstOrDefault(x => x.Id == assignment.TeacherId)?.FullName ?? "Atanmadi";
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '-');
        return value.Replace(' ', '-').ToLowerInvariant();
    }

    private static int DayOrder(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => 1,
        DayOfWeek.Tuesday => 2,
        DayOfWeek.Wednesday => 3,
        DayOfWeek.Thursday => 4,
        DayOfWeek.Friday => 5,
        _ => 9
    };

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Sali",
        DayOfWeek.Wednesday => "Carsamba",
        DayOfWeek.Thursday => "Persembe",
        DayOfWeek.Friday => "Cuma",
        _ => day.ToString()
    };

    private static string ShortDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pa",
        DayOfWeek.Tuesday => "Sa",
        DayOfWeek.Wednesday => "Ca",
        DayOfWeek.Thursday => "Pe",
        DayOfWeek.Friday => "Cu",
        _ => day.ToString()[..2]
    };

    private static string ShortCourseName(string name)
    {
        var normalized = name.Trim();
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mobil Uygulamalar"] = "MOBIL",
            ["Nesne Tabanli Programlama"] = "NTP",
            ["Nesne Tabanlı Programlama"] = "NTP",
            ["Grafik ve Canlandirma"] = "GRAFIK",
            ["Grafik ve Canlandırma"] = "GRAFIK",
            ["Web Tabanli Uygulama Gelistirme"] = "WEB",
            ["Web Tabanlı Uygulama Geliştirme"] = "WEB",
            ["Bilisim Teknik Temelleri"] = "BTT",
            ["Bilişim Teknik Temelleri"] = "BTT"
        };

        if (known.TryGetValue(normalized, out var shortName)) return shortName;
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return normalized;
        if (words.Length == 1) return words[0].Length <= 8 ? words[0].ToUpperInvariant() : words[0][..Math.Min(8, words[0].Length)].ToUpperInvariant();
        return string.Concat(words.Take(3).Select(x => char.ToUpperInvariant(x[0])));
    }

    private static string TeacherInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.TakeLast(Math.Min(2, parts.Length)).Select(x => char.ToUpperInvariant(x[0])));
    }

    private sealed record Row(string DayName, int LessonNumber, string ClassName, string CourseName, string TeacherName, string ResourceName, string Status);
    private sealed record TeacherItem(Guid Id, string Name);
    private sealed record TimeColumn(int LessonNumber, string Title, string Time, bool IsBreak);
    private sealed record PlanEntry(LessonAssignment Assignment, LessonRequest? Request);
    private sealed record PlanSheet(string Title, bool IsResourcePlan, IReadOnlyList<PlanEntry> Entries);
}
