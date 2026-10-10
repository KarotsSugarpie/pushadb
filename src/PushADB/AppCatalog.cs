using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;

namespace PushADB;

internal sealed partial class MainForm
{
    private sealed record AppEntry(string Name, string Category, string OfficialPage, string? DirectApk, string Notes);
    private readonly ComboBox catalogCategory = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox catalogApp = new() { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox catalogAutoInstall = new() { Text = "Installer après téléchargement", Checked = true, AutoSize = true };
    private readonly CheckBox catalogKeepData = new() { Text = "Conserver les données (-r)", Checked = true, AutoSize = true };
    private readonly Label catalogNote = new() { AutoSize = true, MaximumSize = new Size(1000, 0), ForeColor = Color.Silver };
    private static readonly HttpClient catalogClient = new(new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 })
    {
        Timeout = TimeSpan.FromMinutes(8)
    };
    private static readonly AppEntry[] CatalogApps = {
        new("TiviMate", "IPTV", "https://tivimate.com/", "https://tivimate.com/apk", "Éditeur officiel. Lecteur IPTV, ne fournit pas de chaînes."),
        new("VLC 64-bit ARM", "Multimédia", "https://images.videolan.org/vlc/download-android.html", "https://download.videolan.org/pub/pub/videolan/vlc-android/last/VLC-Android-3.7.1-arm64-v8a.apk", "APK officiel VideoLAN, pour les appareils arm64-v8a."),
        new("VLC 32-bit ARM", "Multimédia", "https://images.videolan.org/vlc/download-android.html", "https://download.videolan.org/pub/pub/videolan/vlc-android/last/VLC-Android-3.7.1-armeabi-v7a.apk", "APK officiel VideoLAN, pour les appareils armeabi-v7a."),
        new("Kodi", "Multimédia", "https://kodi.tv/download/android/", null, "Choisir ARMV7A ou ARMV8A sur le site officiel."),
        new("SmartTube", "Multimédia", "https://github.com/yuliskov/SmartTube/releases", null, "Choisir une version APK officielle compatible depuis les releases."),
        new("F-Droid", "Utilitaires", "https://f-droid.org/en/packages/org.fdroid.fdroid/", null, "Magasin d'applications libres, APK disponibles sur le site."),
        new("Downloader by AFTVnews", "Utilitaires", "https://www.aftvnews.com/downloader/", null, "Téléchargeur pour Fire TV et Android TV."),
        new("Send Files to TV", "Utilitaires", "https://sendfilestotv.app/", null, "Transfert local de fichiers."),
        new("Jellyfin", "Multimédia", "https://jellyfin.org/downloads/", null, "Client de bibliothèque multimédia personnelle."),
        new("Plex", "Multimédia", "https://www.plex.tv/media-server-downloads/", null, "Client Plex. Vérifier la disponibilité pour l'appareil."),
        new("YouTube", "Streaming", "https://play.google.com/store/apps/details?id=com.google.android.youtube.tv", null, "Installation par Google Play ou boutique compatible."),
        new("Netflix", "Streaming", "https://www.netflix.com/", null, "Utiliser la boutique officielle certifiée pour la compatibilité DRM."),
        new("Disney+", "Streaming", "https://www.disneyplus.com/", null, "Utiliser la boutique officielle certifiée."),
        new("Spotify", "Audio", "https://www.spotify.com/", null, "Utiliser la boutique officielle compatible.")
    };

    private GroupBox BuildCatalogPanel()
    {
        var panel = Group("CATALOGUE D'APPLICATIONS — SOURCE OFFICIELLE");
        var first = Row();
        first.Height = 57;
        first.Controls.Add(L("Catégorie"));
        catalogCategory.Items.AddRange(CatalogApps.Select(a => a.Category).Distinct().Cast<object>().ToArray());
        catalogCategory.SelectedIndexChanged += (_, _) => RefreshCatalog();
        first.Controls.Add(catalogCategory);
        first.Controls.Add(L("Application"));
        first.Controls.Add(catalogApp);
        first.Controls.Add(B("Site officiel", () => { OpenCatalogPage(); return Task.CompletedTask; }));
        first.Controls.Add(B("Télécharger APK", DownloadCatalog));
        panel.Controls.Add(first);

        var second = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, WrapContents = false, AutoScroll = true };
        second.Controls.Add(catalogAutoInstall);
        second.Controls.Add(catalogKeepData);
        second.Controls.Add(B("Installer APK téléchargé", InstallCatalogApk));
        second.Controls.Add(B("Ouvrir dossier APK", () =>
        {
            Directory.CreateDirectory(CatalogDirectory());
            Process.Start(new ProcessStartInfo("explorer.exe", CatalogDirectory()) { UseShellExecute = true });
            return Task.CompletedTask;
        }));
        panel.Controls.Add(second);
        catalogNote.Dock = DockStyle.Bottom;
        catalogNote.Height = 36;
        panel.Controls.Add(catalogNote);
        catalogApp.SelectedIndexChanged += (_, _) =>
        {
            var app = SelectedCatalog();
            if (app != null) catalogNote.Text = app.Notes + (app.DirectApk == null ? " Téléchargement manuel depuis l'éditeur." : " Téléchargement APK direct disponible.");
        };
        catalogCategory.SelectedIndex = 0;
        return panel;
    }

    private void RefreshCatalog()
    {
        catalogApp.Items.Clear();
        foreach (var app in CatalogApps.Where(a => a.Category == catalogCategory.SelectedItem?.ToString()))
            catalogApp.Items.Add(app.Name);
        if (catalogApp.Items.Count > 0) catalogApp.SelectedIndex = 0;
    }
    private AppEntry? SelectedCatalog() => CatalogApps.FirstOrDefault(a => a.Name == catalogApp.SelectedItem?.ToString());
    private static string CatalogDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PushADB", "APKs");
    private string CatalogPath(AppEntry app) => Path.Combine(CatalogDirectory(), app.Name.Replace(" ", "-").Replace("+", "plus") + ".apk");
    private void OpenCatalogPage()
    {
        var app = SelectedCatalog() ?? throw new InvalidOperationException("Choisir une application.");
        Process.Start(new ProcessStartInfo(app.OfficialPage) { UseShellExecute = true });
        Append("Page officielle ouverte : " + app.OfficialPage);
    }
    private async Task DownloadCatalog()
    {
        var app = SelectedCatalog() ?? throw new InvalidOperationException("Choisir une application.");
        if (app.DirectApk == null)
        {
            OpenCatalogPage();
            MessageBox.Show(this, "Cet éditeur ne fournit pas ici de lien APK direct vérifié. Télécharge l'APK depuis son site officiel, puis utilise Parcourir pour l'installer.", "Téléchargement manuel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var endpoint = catalogAutoInstall.Checked ? RequireDevice() : null;
        Directory.CreateDirectory(CatalogDirectory());
        var output = CatalogPath(app);
        var temporary = output + ".partial";
        Append("Téléchargement officiel : " + app.Name);
        try
        {
            using var response = await catalogClient.GetAsync(app.DirectApk, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var destination = response.RequestMessage?.RequestUri;
            if (destination == null || destination.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Redirection non sécurisée.");
            if (response.Content.Headers.ContentLength is > 250000000)
                throw new InvalidOperationException("Fichier trop volumineux (>250 Mo).");
            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[65536];
                long total = 0;
                int count;
                while ((count = await stream.ReadAsync(buffer)) > 0)
                {
                    total += count;
                    if (total > 250000000) throw new InvalidOperationException("Téléchargement supérieur à 250 Mo.");
                    await file.WriteAsync(buffer.AsMemory(0, count));
                }
            }
            await using (var file = File.OpenRead(temporary))
            {
                Span<byte> hdr = stackalloc byte[4];
                if (file.Read(hdr) != 4 || hdr[0] != 0x50 || hdr[1] != 0x4B)
                    throw new InvalidOperationException("La réponse du site n'est pas une archive APK valide. Aucun fichier ne sera installé.");
            }
            File.Move(temporary, output, true);
            Append("APK téléchargé : " + output);
            apk.Text = output;
            if (catalogAutoInstall.Checked && endpoint != null)
                await InstallDownloaded(endpoint, output);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
    private async Task InstallCatalogApk()
    {
        var app = SelectedCatalog() ?? throw new InvalidOperationException("Choisir une application.");
        var output = CatalogPath(app);
        if (!File.Exists(output)) throw new InvalidOperationException("APK non téléchargé : " + output);
        await InstallDownloaded(RequireDevice(), output);
    }
    private async Task InstallDownloaded(string endpoint, string output)
    {
        if (MessageBox.Show(this,
            $"Installer {Path.GetFileName(output)} sur {endpoint} ?\n\nConserver les données : {(catalogKeepData.Checked ? "oui (-r)" : "non (installation sans -r, ne supprime pas les données)")}",
            "Installation ADB", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var options = new List<string> { "-s", endpoint, "install" };
        if (catalogKeepData.Checked) options.Add("-r");
        options.Add(output);
        var result = await Adb(options.ToArray());
        Append("Installation : " + result.Output);
        if (result.ExitCode != 0 || !result.Output.Split('\n').Any(line => line.Trim() == "Success"))
            throw new InvalidOperationException("ADB n'a pas confirmé l'installation. Voir journal.");
        Append("Installation réussie sur " + endpoint);
    }
}
