using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace DersDagitim.Wpf;

public partial class AboutUpdateWindow : Window
{
    public AboutUpdateWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Surum {GetVersion()}";
        UpdateStatusText.Text = "Guncelleme kanali: yerel update-manifest.json / www.ikizsoft.com";
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

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateStatusText.Text = "Guncelleme manifesti okunuyor...";
            var manifest = await ReadManifestAsync();
            if (manifest is null)
            {
                UpdateStatusText.Text = "Guncelleme manifesti bulunamadi. Uygulama klasorunde update-manifest.json bekleniyor.";
                return;
            }

            var local = NormalizeVersion(GetVersion());
            var latest = NormalizeVersion(manifest.LatestVersion);
            var status = CompareVersions(local, latest) < 0
                ? $"Yeni surum var: {manifest.LatestVersion}. Indirme: {manifest.DownloadUrl}"
                : $"Uygulama guncel. Yerel surum: {GetVersion()}, manifest surumu: {manifest.LatestVersion}.";

            if (!string.IsNullOrWhiteSpace(manifest.ReleaseNotes))
            {
                status += $"{Environment.NewLine}Notlar: {manifest.ReleaseNotes}";
            }

            UpdateStatusText.Text = status;
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Guncelleme kontrolu basarisiz: {ex.Message}";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static async Task<UpdateManifest?> ReadManifestAsync()
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "update-manifest.json");
        if (!File.Exists(localPath)) return null;

        await using var stream = File.OpenRead(localPath);
        return await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static Version NormalizeVersion(string value)
    {
        var clean = value.Split('+')[0].Split('-')[0].Trim();
        return Version.TryParse(clean, out var version) ? version : new Version(0, 0, 0, 0);
    }

    private static int CompareVersions(Version local, Version latest)
    {
        var localNormalized = new Version(local.Major, local.Minor, Math.Max(local.Build, 0), Math.Max(local.Revision, 0));
        var latestNormalized = new Version(latest.Major, latest.Minor, Math.Max(latest.Build, 0), Math.Max(latest.Revision, 0));
        return localNormalized.CompareTo(latestNormalized);
    }

    private sealed record UpdateManifest(string LatestVersion, string DownloadUrl, string ReleaseNotes);
}
