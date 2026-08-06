using System.Windows;
using System.Windows.Controls;

namespace DersDagitim.Wpf;
public sealed class TeacherWebPreviewWindow : Window
{
    public TeacherWebPreviewWindow()
    {
        Title = "Web listesinden öğretmen önizlemesi - aktarım yapılmadı"; Width = 850; Height = 680; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new Thickness(20) }; Content = panel;
        var info = new TextBlock { Text = "Kaynak: Tokat Dinamik Okulları Kadromuz\nURL: https://tokat.dinamikokullari.com/kadromuz\nErişim: 02.08.2026 · Yalnızca önizleme; veritabanına aktarım yapılmadı.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) }; DockPanel.SetDock(info, Dock.Top); panel.Children.Add(info);
        var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false }; grid.Columns.Add(new DataGridTextColumn { Header = "Ad Soyad", Binding = new System.Windows.Data.Binding("Name"), Width = 260 }); grid.Columns.Add(new DataGridTextColumn { Header = "Kısa kod", Binding = new System.Windows.Data.Binding("Code"), Width = 90 }); grid.Columns.Add(new DataGridTextColumn { Header = "Branş", Binding = new System.Windows.Data.Binding("Branch"), Width = 300 }); grid.ItemsSource = Rows(); panel.Children.Add(grid);
    }
    private static IReadOnlyList<Row> Rows() => TeacherWebDirectory.Teachers.Select(x => new Row(x.FullName, x.Code ?? "", x.Department ?? "")).ToArray();
    private sealed record Row(string Name, string Code, string Branch);
}
