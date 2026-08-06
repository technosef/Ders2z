using System.Windows;
using System.Windows.Controls;
using DersDagitim.Domain;

namespace DersDagitim.Wpf;

public sealed class EntityEditorWindow : Window
{
    private readonly string _kind; private readonly Guid _id; private readonly Dictionary<string, TextBox> _fields = new(); private readonly ComboBox _type = null!;
    public object? Value { get; private set; }
    public EntityEditorWindow(string kind, object? source = null)
    {
        _kind = kind; _id = source switch { Teacher x => x.Id, SchoolClass x => x.Id, Course x => x.Id, Resource x => x.Id, _ => Guid.NewGuid() };
        Title = source is null ? $"Yeni {Label(kind)}" : $"{Label(kind)} düzenle"; Width = 460; Height = kind == "course" ? 480 : 390; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(22) }; Content = panel;
        AddText(panel, "Ad / ad soyad", "name", Get(source, "name"));
        if (kind == "teacher") { AddText(panel, "Kısa kod", "code", Get(source, "code")); AddText(panel, "Haftalık üst sınır", "hours", Get(source, "hours", "30")); AddText(panel, "Alan / branş (isteğe bağlı)", "department", Get(source, "department")); AddText(panel, "Uygunluk / kilit notu (isteğe bağlı)", "availability", Get(source, "availability")); _type = new ComboBox { ItemsSource = new[] { "Kadrolu", "Dışarıdan ders veren", "Ücretli" }, SelectedIndex = source is Teacher t ? Array.IndexOf(new[] { "Kadrolu", "Dışarıdan ders veren", "Ücretli" }, t.StaffStatus) : 0, Margin = new Thickness(0, 5, 0, 8) }; panel.Children.Add(new TextBlock { Text = "Kadro durumu / çalışma türü" }); panel.Children.Add(_type); }
        if (kind == "class") { AddText(panel, "Sınıf düzeyi", "grade", Get(source, "grade")); AddText(panel, "Şube", "branch", Get(source, "branch")); AddText(panel, "Alan", "department", Get(source, "department")); _type = new ComboBox { ItemsSource = new[] { "Anadolu Meslek Programı", "Anadolu Teknik Programı" }, SelectedIndex = source is SchoolClass c ? (int)c.ProgramType : 0, Margin = new Thickness(0, 5, 0, 8) }; panel.Children.Add(new TextBlock { Text = "Program türü" }); panel.Children.Add(_type); }
        if (kind == "course") { AddText(panel, "Kısaltma", "abbr", Get(source, "abbr")); AddText(panel, "Haftalık saat", "hours", Get(source, "hours", "1")); AddText(panel, "Blok seçenekleri (örn. 5,3+2)", "blocks", Get(source, "blocks", "1")); AddText(panel, "Kaynak / sürüm", "source", "Deneme - MEB kaynağı doğrulanmadı"); }
        if (kind == "resource") { AddText(panel, "Kapasite", "capacity", Get(source, "capacity", "1")); _type = new ComboBox { ItemsSource = new[] { "Derslik", "Atölye", "Laboratuvar" }, SelectedIndex = source is Resource r ? (int)r.Type : 0, Margin = new Thickness(0, 5, 0, 8) }; panel.Children.Add(new TextBlock { Text = "Kaynak türü" }); panel.Children.Add(_type); }
        var save = new Button { Content = "Kaydet", Padding = new Thickness(14, 7, 14, 7), HorizontalAlignment = HorizontalAlignment.Right }; save.Click += Save_Click; panel.Children.Add(save);
    }
    private void AddText(Panel panel, string label, string key, string value) { panel.Children.Add(new TextBlock { Text = label }); var box = new TextBox { Text = value, Margin = new Thickness(0, 2, 0, 7) }; _fields[key] = box; panel.Children.Add(box); }
    private static string Get(object? source, string key, string fallback = "") => key switch { "name" => source switch { Teacher x => x.FullName, SchoolClass x => x.Name, Course x => x.Name, Resource x => x.Name, _ => fallback }, "code" => (source as Teacher)?.Code ?? fallback, "hours" => source switch { Teacher x => x.WeeklyMaximumHours.ToString(), Course x => x.WeeklyHours.ToString(), _ => fallback }, "grade" => (source as SchoolClass)?.Grade.ToString() ?? fallback, "branch" => (source as SchoolClass)?.Branch ?? fallback, "department" => source switch { Teacher x => x.Department ?? fallback, SchoolClass x => x.Department ?? fallback, _ => fallback }, "availability" => (source as Teacher)?.AvailabilityNote ?? fallback, "abbr" => (source as Course)?.Abbreviation ?? fallback, "blocks" => source is Course c ? string.Join(',', c.BlockOptions) : fallback, "capacity" => (source as Resource)?.Capacity.ToString() ?? fallback, _ => fallback };
    private static string Label(string kind) => kind switch { "teacher" => "öğretmen", "class" => "sınıf / şube", "course" => "ders", _ => "kaynak" };
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string V(string key) => _fields.TryGetValue(key, out var x) ? x.Text.Trim() : ""; if (string.IsNullOrWhiteSpace(V("name"))) { MessageBox.Show("Ad alanı boş bırakılamaz.", "Eksik bilgi", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { Value = _kind switch { "teacher" => new Teacher(_id, V("name"), int.Parse(V("hours")), Code: V("code"), Department: V("department"), AvailabilityNote: V("availability"), StaffStatus: _type.SelectedItem?.ToString() ?? "Kadrolu"), "class" => new SchoolClass(_id, V("name"), int.Parse(V("grade")), V("branch"), V("department"), true, (SchoolProgramType)_type.SelectedIndex), "course" => new Course(_id, V("name"), int.Parse(V("hours")), V("blocks").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), Abbreviation: V("abbr"), SourceLabel: V("source"), IsDemo: true), _ => new Resource(_id, V("name"), int.Parse(V("capacity")), (ResourceType)_type.SelectedIndex) }; DialogResult = true; } catch (Exception ex) { MessageBox.Show($"Bilgiler geçerli değil: {ex.Message}", "Kayıt doğrulama", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
