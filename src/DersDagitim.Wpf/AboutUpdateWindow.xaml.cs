using System.Reflection;
using System.Windows;

namespace DersDagitim.Wpf;

public partial class AboutUpdateWindow : Window
{
    public AboutUpdateWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Sürüm {GetVersion()}";
        UpdateStatusText.Text = "Güncelleme kanalı: www.ikizsoft.com";
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "bilinmiyor";
    }

    private void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = $"Yerel sürüm {GetVersion()} kullanılıyor. Otomatik güncelleme sunucusu henüz bağlı değil; güncel paket kontrolü www.ikizsoft.com üzerinden yapılacak.";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
