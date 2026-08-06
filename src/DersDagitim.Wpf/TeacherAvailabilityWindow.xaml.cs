using System.Windows; using DersDagitim.Domain;
namespace DersDagitim.Wpf;
public partial class TeacherAvailabilityWindow : Window
{
    public TeacherAvailabilityWindow()
    {
        InitializeComponent();
        // Kısıtlar henüz gerçek SQLite kayıtlarından bağlanmadı; demo satırı göstermemek için boş başlatılır.
        AvailabilityGrid.ItemsSource = Array.Empty<Row>();
    }

    private sealed class Row(string teacher, string day, int lesson, RestrictionType type, AvailabilitySeverity severity, string reason)
    {
        public string TeacherName => teacher;
        public string Day => day;
        public int LessonNumber => lesson;
        public string Status => severity == AvailabilitySeverity.HardLock ? "Kesin kilit" : type == RestrictionType.PreferredFree ? "Tercih edilen boşluk" : "Düşük öncelik";
        public string Reason => reason;
        public string Recurrence => "Her hafta";
    }
}
