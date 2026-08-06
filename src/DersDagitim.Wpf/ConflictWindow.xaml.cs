using System.Windows;
namespace DersDagitim.Wpf;
public partial class ConflictWindow : Window
{
    public ConflictWindow() { InitializeComponent(); var result = DraftWorkspace.Current; SummaryText.Text = result is null ? "Taslak üretmeden önce kontrol edilecek sonuç yok." : $"Çakışma / yerleşemeyen talep sayısı: {result.Unassigned.Count}"; ConflictsGrid.ItemsSource = result?.Unassigned; }
}
