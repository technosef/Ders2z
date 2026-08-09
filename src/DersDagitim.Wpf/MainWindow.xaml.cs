using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace DersDagitim.Wpf;
public partial class MainWindow : Window
{
    private CheckBox UseAscCardsCheckBox = null!;
    public MainWindow() { InitializeComponent(); AddDraftOptions(); Loaded += async (_, _) => await LoadDashboardAsync(); }
    private void AddDraftOptions() { UseAscCardsCheckBox = new CheckBox { Content = "Mevcut ASC kartlarını koru", IsChecked = true, Margin = new Thickness(12, 8, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White }; var contentGrid = (Grid)((Grid)Content).Children[1]; var header = (DockPanel)contentGrid.Children[0]; header.Children.Insert(0, UseAscCardsCheckBox); }
    private async Task LoadDashboardAsync() { try { var s = await App.Dashboard.LoadAsync(); ClassCount.Text = s.ClassCount.ToString(); TeacherCount.Text = s.TeacherCount.ToString(); CourseCount.Text = s.CourseCount.ToString(); StatusText.Text = "Yerel SQLite deposu hazır"; } catch (Exception ex) { StatusText.Text = $"Veri deposu başlatılamadı: {ex.Message}"; } }
    private void Dashboard_Click(object sender, RoutedEventArgs e) { }
    private void Schedule_Click(object sender, RoutedEventArgs e) => new LaboratoryScheduleWindow { Owner = this }.ShowDialog();
    private void Entity_Click(object sender, RoutedEventArgs e) => new DataManagementWindow { Owner = this }.ShowDialog();
    private void Settings_Click(object sender, RoutedEventArgs e) => new TeacherAvailabilityWindow { Owner = this }.ShowDialog();
    private async void Draft_Click(object sender, RoutedEventArgs e) { try { var protectedCards = UseAscCardsCheckBox.IsChecked == true; StatusText.Text = "ASC ders talepleriyle taslak üretiliyor..."; await DraftWorkspace.GenerateAsync(App.Dashboard.Repository, protectedCards); StatusText.Text = $"Gerçek ASC taslağı hazır · {DraftWorkspace.MappedLessonCount}/{DraftWorkspace.ImportedLessonCount} talep · {DraftWorkspace.ProtectedCardCount}/{DraftWorkspace.ImportedCardCount} korunan kart · {DraftWorkspace.Current!.Unassigned.Count} yerleşmeyen"; new DraftScheduleWindow { Owner = this }.ShowDialog(); } catch (Exception ex) { StatusText.Text = $"Taslak üretilemedi: {ex.Message}"; } }
    private void Conflict_Click(object sender, RoutedEventArgs e) => new ConflictWindow { Owner = this }.ShowDialog();
    private void Report_Click(object sender, RoutedEventArgs e) => new TeacherReportWindow { Owner = this }.ShowDialog();
    private void SchoolOverview_Click(object sender, RoutedEventArgs e) => new SchoolOverviewWindow { Owner = this }.ShowDialog();
    private void AboutUpdate_Click(object sender, RoutedEventArgs e) => new AboutUpdateWindow { Owner = this }.ShowDialog();
}
