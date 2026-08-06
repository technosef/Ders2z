using System.IO;
using System.Windows;
using DersDagitim.Application;
using DersDagitim.Infrastructure;

namespace DersDagitim.Wpf;

public partial class App : System.Windows.Application
{
    public static DashboardService Dashboard { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => { WriteStartupLog(args.Exception); args.Handled = true; };
        try
        {
            var dir = GetWritableDataDirectory();
            Dashboard = new DashboardService(new SqliteSchoolRepository(Path.Combine(dir, "ders-dagitim.db")));
            Dashboard.LoadAsync().GetAwaiter().GetResult();
            new MainWindow().Show();
        }
        catch (Exception exception)
        {
            var logPath = WriteStartupLog(exception);
            MessageBox.Show($"Uygulama başlatılamadı. Ayrıntı: {logPath}\n\n{exception.GetType().Name}: {exception.Message}", "Ders Dağıtım Uygulaması", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static string WriteStartupLog(Exception exception)
    {
        var content = $"{DateTimeOffset.Now:O}\n{exception}\n";
        foreach (var path in new[] { Path.Combine(Path.GetTempPath(), "DersDagitim-startup-error.log"), Path.Combine(Environment.CurrentDirectory, "startup-error.log"), Path.Combine(AppContext.BaseDirectory, "startup-error.log") })
        {
            try { File.WriteAllText(path, content); return path; } catch (UnauthorizedAccessException) { }
        }
        return "log dosyası yazılamadı";
    }

    private static string GetWritableDataDirectory()
    {
        var preferred = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DersDagitim");
        try { Directory.CreateDirectory(preferred); return preferred; }
        catch (UnauthorizedAccessException) { var fallback = Path.Combine(AppContext.BaseDirectory, "Data"); Directory.CreateDirectory(fallback); return fallback; }
    }
}
