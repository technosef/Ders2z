using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using DersDagitim.Application;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public partial class SchoolOverviewWindow : Window
{
    private IReadOnlyList<SchoolClass> _classes = Array.Empty<SchoolClass>();
    private IReadOnlyList<AscScheduleCard> _cards = Array.Empty<AscScheduleCard>();
    private bool _filtersReady;

    public SchoolOverviewWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _classes = await App.Dashboard.Repository.GetClassesAsync();
        _cards = await App.Dashboard.Repository.GetAscScheduleCardsAsync();
        FillFilters();
        ApplyFilter();
    }

    private void FillFilters()
    {
        _filtersReady = false;
        SetItems(DepartmentFilter, "Alan: Tümü", _classes.Select(x => x.Department).Where(x => !string.IsNullOrWhiteSpace(x))!);
        SetItems(ProgramFilter, "Program: Tümü", _classes.Select(x => ProgramText(x.ProgramType)));
        SetItems(ClassFilter, "Sınıf/Şube: Tümü", _classes.Select(x => x.Name));
        SetItems(TeacherFilter, "Öğretmen: Tümü", _cards.Select(x => x.TeacherName));
        SetItems(ResourceFilter, "Kaynak: Tümü", _cards.Select(x => x.ResourceName).Where(x => !string.IsNullOrWhiteSpace(x)));
        _filtersReady = true;
    }

    private static void SetItems(ComboBox combo, string allText, IEnumerable<string> values)
    {
        combo.ItemsSource = new[] { allText }.Concat(values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)).ToArray();
        combo.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        if (!_filtersReady) return;

        var department = SelectedValue(DepartmentFilter, "Alan: Tümü");
        var program = SelectedValue(ProgramFilter, "Program: Tümü");
        var className = SelectedValue(ClassFilter, "Sınıf/Şube: Tümü");
        var teacher = SelectedValue(TeacherFilter, "Öğretmen: Tümü");
        var resource = SelectedValue(ResourceFilter, "Kaynak: Tümü");
        var classByName = _classes
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var rows = _cards
            .Select(card => (Card: card, Class: classByName.TryGetValue(card.ClassName, out var schoolClass) ? schoolClass : null))
            .Where(x => department is null || string.Equals(x.Class?.Department, department, StringComparison.OrdinalIgnoreCase))
            .Where(x => program is null || string.Equals(x.Class is null ? "" : ProgramText(x.Class.ProgramType), program, StringComparison.OrdinalIgnoreCase))
            .Where(x => className is null || string.Equals(x.Card.ClassName, className, StringComparison.OrdinalIgnoreCase))
            .Where(x => teacher is null || string.Equals(x.Card.TeacherName, teacher, StringComparison.OrdinalIgnoreCase))
            .Where(x => resource is null || string.Equals(x.Card.ResourceName, resource, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => DayOrder(x.Card.DayName))
            .ThenBy(x => x.Card.Period)
            .ThenBy(x => x.Card.ClassName)
            .Select(x => new OverviewRow(
                x.Card.DayName,
                x.Card.ClassName,
                $"{x.Card.Period}. ders",
                x.Class?.Department ?? "-",
                $"{x.Card.CourseName} · {x.Card.TeacherName}{(string.IsNullOrWhiteSpace(x.Card.ResourceName) ? "" : " · " + x.Card.ResourceName)}",
                x.Card.IsRemoved ? "Manuel kaldırıldı" : x.Card.IsManualOverride ? "Manuel değiştirildi" : "ASC kartı"))
            .ToArray();

        OverviewGrid.ItemsSource = rows;
        SummaryText.Text = $"Kurum ASC görünümü: {rows.Length}/{_cards.Count} kart gösteriliyor · {_classes.Count} sınıf";
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void ExportColored_Click(object sender, RoutedEventArgs e)
    {
        if (_cards.Count == 0)
        {
            MessageBox.Show("Renkli okul dosyası üretmek için önce ASC kartları yüklenmelidir.", "Okul genel görünümü", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog { Filter = "HTML dosyası|*.html", FileName = "okul-genel-renkli-program.html" };
        if (dialog.ShowDialog() != true) return;

        var html = BuildColoredHtml();
        File.WriteAllText(dialog.FileName, html, new System.Text.UTF8Encoding(true));
        MessageBox.Show("Renkli okul genel program dosyası üretildi. Tarayıcıda açıp yazdırabilir veya PDF'e çevirebilirsiniz.", "Okul genel görünümü", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string BuildColoredHtml()
    {
        var days = new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
        var bySlot = _cards.Where(x => !x.IsRemoved).GroupBy(x => (x.DayName, x.Period)).ToDictionary(x => x.Key, x => x.ToArray());
        var body = new System.Text.StringBuilder();
        body.AppendLine("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>Okul Genel Renkli Program</title>");
        body.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#0f172a}table{border-collapse:collapse;width:100%;table-layout:fixed}th,td{border:1px solid #cbd5e1;padding:8px;vertical-align:top;font-size:12px}th{background:#e2e8f0}.slot{width:64px;background:#f8fafc;font-weight:700}.card{margin:2px 0;padding:4px;border-radius:4px;color:#111827;break-inside:avoid}.muted{color:#64748b}.count{font-weight:700;margin-bottom:4px}@media print{body{margin:8mm}td,th{font-size:10px}}</style></head><body>");
        body.AppendLine($"<h1>Okul Genel Renkli Program</h1><p class=\"muted\">{_cards.Count(x => !x.IsRemoved)} aktif kart · {DateTime.Now:dd.MM.yyyy HH:mm}</p><table><thead><tr><th class=\"slot\">Saat</th>");
        foreach (var day in days) body.AppendLine($"<th>{Escape(day)}</th>");
        body.AppendLine("</tr></thead><tbody>");
        for (var period = 1; period <= 10; period++)
        {
            body.AppendLine($"<tr><td class=\"slot\">{period}</td>");
            foreach (var day in days)
            {
                body.Append("<td>");
                if (bySlot.TryGetValue((day, period), out var cards))
                {
                    body.AppendLine($"<div class=\"count\">{cards.Length} ders</div>");
                    foreach (var card in cards.OrderBy(x => x.ClassName).ThenBy(x => x.TeacherName))
                    {
                        var color = string.IsNullOrWhiteSpace(card.TeacherColor) ? "#dbeafe" : card.TeacherColor;
                        body.AppendLine($"<div class=\"card\" style=\"background:{Escape(color)}\"><b>{Escape(card.ClassName)}</b> · {Escape(card.CourseName)}<br>{Escape(card.TeacherName)}<br><span class=\"muted\">{Escape(card.ResourceName)}</span></div>");
                    }
                }
                body.AppendLine("</td>");
            }
            body.AppendLine("</tr>");
        }
        body.AppendLine("</tbody></table></body></html>");
        return body.ToString();
    }

    private static string Escape(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");
    private static string? SelectedValue(ComboBox combo, string allText) => combo.SelectedItem is string value && value != allText ? value : null;
    private static string ProgramText(SchoolProgramType type) => type == SchoolProgramType.AnadoluTeknikProgrami ? "Anadolu Teknik Programı" : "Anadolu Meslek Programı";
    private static int DayOrder(string day) => day switch { "Pazartesi" => 1, "Salı" => 2, "Çarşamba" => 3, "Perşembe" => 4, "Cuma" => 5, _ => 9 };
    private sealed record OverviewRow(string Day, string Group, string Slot, string Department, string Display, string Status);
}
