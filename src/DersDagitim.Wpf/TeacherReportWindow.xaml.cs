using System.Windows; using DersDagitim.Domain;
namespace DersDagitim.Wpf;
public partial class TeacherReportWindow : Window
{
    public TeacherReportWindow() { InitializeComponent(); var report = TeacherReportSample.Create(); SchoolText.Text = report.SchoolName; TeacherText.Text = report.Teacher.FullName; YearText.Text = report.AcademicYear; AssignmentsGrid.ItemsSource = report.Assignments.Select(x => new AssignmentRow(x)).Concat(report.VocationalTraining!.Select(x => new AssignmentRow(x))).ToArray(); DutiesGrid.ItemsSource = report.Duties; VocationalText.Text = $"İşletmelerde Mesleki Eğitim: {report.VocationalTraining!.Sum(x => x.WeeklyHours)} saat"; TotalText.Text = $"Toplam: {report.TotalHours} saat"; }
    private void Export_Click(object sender, RoutedEventArgs e) => MessageBox.Show("PDF ve Excel dışa aktarımı için rapor şablonu hazırlandı; dosya üretimi sonraki aşamada bağlanacak.", "Dışa aktarım", MessageBoxButton.OK, MessageBoxImage.Information);
    private sealed class AssignmentRow { public string CourseName { get; } public int WeeklyHours { get; } public string ClassNamesText { get; } public AssignmentRow(TeacherAssignmentSummary source) { CourseName = source.CourseName; WeeklyHours = source.WeeklyHours; ClassNamesText = string.Join(", ", source.ClassNames); } public AssignmentRow(WorkplaceVocationalTrainingAssignment source) { CourseName = "İşletmelerde Mesleki Eğitim"; WeeklyHours = source.WeeklyHours; ClassNamesText = $"{source.ClassName} · {source.WorkplaceGroup} · {source.TermOrAcademicYear}"; } }
}
