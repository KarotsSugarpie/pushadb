using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PushADB;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox ip = new() { Text = "192.168.254.161", Width = 170 };
    private readonly NumericUpDown port = new() { Minimum = 1, Maximum = 65535, Value = 5555, Width = 72 };
    private readonly TextBox apk = new() { Width = 440, ReadOnly = true };
    private readonly TextBox package = new() { Text = "ar.tvplayer.tv", Width = 230 };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.FromArgb(13, 21, 33), ForeColor = Color.FromArgb(213, 227, 240), Font = new Font("Consolas", 9.5f) };
    private readonly Label status = new() { Text = "Non connecté", AutoSize = true, ForeColor = Color.Gold };
    private readonly Label deviceInfo = new() { Text = "Appareil : aucun", AutoSize = true };
    private readonly ComboBox preset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
    private readonly List<Button> actionButtons = new();
    private readonly string adbPath;
    private string? selectedDevice;
    private bool busy;
    private readonly SemaphoreSlim adbLock = new(1, 1);

    public MainForm()
    {
        Text = "PushADB  •  Android TV / Fire TV Manager";
        MinimumSize = new Size(930, 680);
        Size = new Size(1080, 770);
        BackColor = Color.FromArgb(23, 32, 48);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterScreen;
        adbPath = ResolveAdb();

        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 135));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(outer);

        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(new Label { Text = "PushADB", Font = new Font("Segoe UI", 25, FontStyle.Bold), AutoSize = true, Location = new Point(1, 0), ForeColor = Color.FromArgb(82, 202, 250) });
        header.Controls.Add(new Label { Text = "Gestionnaire ADB pour Fire TV et Google / Android TV", AutoSize = true, Location = new Point(3, 51), ForeColor = Color.Silver });
        outer.Controls.Add(header, 0, 0);

        var connection = Group("CONNEXION");
        var connectionRow = Row();
        connectionRow.Controls.Add(L("Adresse IP")); connectionRow.Controls.Add(ip);
        connectionRow.Controls.Add(L("Port")); connectionRow.Controls.Add(port);
        connectionRow.Controls.Add(B("Connecter", async () => await Connect()));
        connectionRow.Controls.Add(B("Déconnecter", async () => await Disconnect()));
        connectionRow.Controls.Add(B("Actualiser", async () => await RefreshDevice()));
        connection.Controls.Add(connectionRow);
        var connectionFooter = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 28, FlowDirection = FlowDirection.LeftToRight };
        connectionFooter.Controls.Add(status); connectionFooter.Controls.Add(new Label { Text = "    |    ", AutoSize = true }); connectionFooter.Controls.Add(deviceInfo);
        connection.Controls.Add(connectionFooter);
        outer.Controls.Add(connection, 0, 1);

        var installation = Group("APPLICATIONS / APK");
        var apkRow = Row();
        apkRow.Controls.Add(L("Fichier APK")); apkRow.Controls.Add(apk);
        apkRow.Controls.Add(B("Parcourir...", PickApk));
        apkRow.Controls.Add(B("Installer / Mettre à jour", async () => await Install()));
        installation.Controls.Add(apkRow);
        var appRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, AutoScroll = true, WrapContents = true, Padding = new Padding(3) };
        appRow.Controls.Add(L("Identifiant package"));
        appRow.Controls.Add(package);
        appRow.Controls.Add(B("Vérifier", async () => await CheckPackage()));
        appRow.Controls.Add(B("Ouvrir", async () => await LaunchPackage()));
        appRow.Controls.Add(B("Infos appareil", async () => await RefreshDevice()));
        installation.Controls.Add(appRow);
        outer.Controls.Add(installation, 0, 2);

        var configuration = Group("CONFIGURATION RAPIDE");
        var configRow = Row();
        preset.Items.AddRange(new object[] {
            "Écran : veille 30 minutes",
            "Écran : veille 5 minutes",
            "Animations : désactiver",
            "Animations : rétablir",
            "Rotation : automatique",
            "Rotation : verrouiller",
            "Accueil : touche HOME",
            "Redémarrer l'appareil"
        });
        preset.SelectedIndex = 0;
        configRow.Controls.Add(preset);
        configRow.Controls.Add(B("Appliquer", async () => await ApplyPreset()));
        configRow.Controls.Add(B("Lister les appareils", async () => await ListDevices()));
        configuration.Controls.Add(configRow);
        configuration.Controls.Add(new Label {
            Text = "Les réglages disponibles dépendent de la version Android / Fire OS et des permissions ADB. Aucune action n'est lancée sans clic.",
            Dock = DockStyle.Bottom, Height = 32, ForeColor = Color.Silver
        });
        outer.Controls.Add(configuration, 0, 3);

        var history = Group("JOURNAL DES OPÉRATIONS");
        history.Controls.Add(log);
        outer.Controls.Add(history, 0, 4);

        FormClosing += (_, _) => adbLock.Dispose();
        Append("PushADB prêt. ADB : " + adbPath);
        Append("Activez le débogage sur l'appareil et acceptez l'empreinte RSA sur le téléviseur.");
    }

    private static FlowLayoutPanel Row() => new() { Dock = DockStyle.Top, Height = 64, AutoScroll = true, WrapContents = true, Padding = new Padding(4, 7, 4, 2) };
    private static Label L(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(7, 11, 5, 0) };
    private static GroupBox Group(string title) => new() { Text = title, Dock = DockStyle.Fill, Padding = new Padding(10, 12, 10, 8), ForeColor = Color.FromArgb(226, 233, 244) };
    private Button B(string title, Action action) => B(title, () => { action(); return Task.CompletedTask; });
    private Button B(string title, Func<Task> action)
    {
        var button = new Button { Text = title, AutoSize = true, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(33, 79, 111), ForeColor = Color.WhiteSmoke, Margin = new Padding(5, 3, 5, 3), Padding = new Padding(8, 0, 8, 0) };
        button.FlatAppearance.BorderColor = Color.FromArgb(63, 121, 154);
        button.Click += async (_, _) => await ExecuteAction(action);
        actionButtons.Add(button);
        return button;
    }
    private async Task ExecuteAction(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        foreach (var b in actionButtons) b.Enabled = false;
        try { await action(); }
        catch (Exception ex) { Append("ERREUR : " + ex.Message); MessageBox.Show(this, ex.Message, "PushADB", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { busy = false; foreach (var b in actionButtons) b.Enabled = true; }
    }
    private void Append(string message)
    {
        if (IsDisposed) return;
        log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
    private static string ResolveAdb()
    {
        var adjacent = Path.Combine(AppContext.BaseDirectory, "platform-tools", "adb.exe");
        if (File.Exists(adjacent)) return adjacent;
        adjacent = Path.Combine(AppContext.BaseDirectory, "adb.exe");
        if (File.Exists(adjacent)) return adjacent;
        if (File.Exists(@"C:\ADB\adb.exe")) return @"C:\ADB\adb.exe";
        return "adb.exe";
    }
    private string Endpoint()
    {
        var address = ip.Text.Trim();
        if (!IPAddress.TryParse(address, out var parsed) || parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new InvalidOperationException("Entre une adresse IPv4 valide (ex. 192.168.254.161).");
        return $"{parsed}:{port.Value}";
    }
    private async Task<(int ExitCode, string Output)> Adb(params string[] args)
    {
        await adbLock.WaitAsync();
        try
        {
            var start = new ProcessStartInfo(adbPath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var arg in args) start.ArgumentList.Add(arg);
            using var process = new Process { StartInfo = start };
            if (!process.Start()) throw new InvalidOperationException("Impossible de démarrer ADB.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new TimeoutException("Commande ADB trop longue (5 minutes).");
            }
            var output = (await stdout + Environment.NewLine + await stderr).Trim();
            return (process.ExitCode, output);
        }
        catch (System.ComponentModel.Win32Exception ex) { throw new InvalidOperationException("ADB introuvable. Vérifie le dossier platform-tools ou C:\\ADB.", ex); }
        finally { adbLock.Release(); }
    }
    private async Task Connect()
    {
        var endpoint = Endpoint();
        var result = await Adb("connect", endpoint);
        Append(result.Output);
        await Probe(endpoint);
    }
    private async Task Disconnect()
    {
        var endpoint = Endpoint();
        var result = await Adb("disconnect", endpoint);
        Append(result.Output);
        selectedDevice = null;
        status.Text = "Déconnecté"; status.ForeColor = Color.Gold;
        deviceInfo.Text = "Appareil : aucun";
    }
    private async Task ListDevices()
    {
        var r = await Adb("devices", "-l");
        Append(r.Output);
    }
    private async Task RefreshDevice() => await Probe(Endpoint());
    private async Task Probe(string endpoint)
    {
        var r = await Adb("devices");
        var line = r.Output.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.StartsWith(endpoint + "\t", StringComparison.OrdinalIgnoreCase) || x.StartsWith(endpoint + " ", StringComparison.OrdinalIgnoreCase));
        var state = line?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "absent";
        if (state != "device")
        {
            selectedDevice = null;
            status.Text = state switch { "unauthorized" => "Autorisation requise", "offline" => "Hors ligne", _ => "Non connecté" };
            status.ForeColor = Color.Gold;
            deviceInfo.Text = "Appareil : aucun";
            Append($"État {endpoint} : {state}. Autorisez ADB depuis le téléviseur si nécessaire.");
            return;
        }
        selectedDevice = endpoint;
        status.Text = "Connecté"; status.ForeColor = Color.LightGreen;
        var model = await Adb("-s", endpoint, "shell", "getprop", "ro.product.model");
        var version = await Adb("-s", endpoint, "shell", "getprop", "ro.build.version.release");
        deviceInfo.Text = $"Appareil : {model.Output.Trim()} / Android {version.Output.Trim()}";
        Append($"Connecté : {endpoint} — {deviceInfo.Text}");
    }
    private string RequireDevice()
    {
        var endpoint = Endpoint();
        if (selectedDevice != endpoint) throw new InvalidOperationException("Connecte et autorise d'abord cet appareil.");
        return endpoint;
    }
    private void PickApk()
    {
        using var picker = new OpenFileDialog { Title = "Choisir un fichier APK", Filter = "Application Android (*.apk)|*.apk", CheckFileExists = true };
        if (picker.ShowDialog(this) == DialogResult.OK) apk.Text = picker.FileName;
    }
    private async Task Install()
    {
        var endpoint = RequireDevice();
        if (!File.Exists(apk.Text)) throw new InvalidOperationException("Choisis d'abord un fichier APK valide.");
        if (MessageBox.Show(this, $"Installer / mettre à jour l'APK sur {endpoint} ?\n\n{Path.GetFileName(apk.Text)}", "Confirmer l'installation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Append("Installation : " + Path.GetFileName(apk.Text));
        var result = await Adb("-s", endpoint, "install", "-r", apk.Text);
        Append(result.Output);
        if (result.ExitCode != 0 || !Regex.IsMatch(result.Output, @"(?m)^Success\s*$"))
            throw new InvalidOperationException("Installation non confirmée. Consulte le journal.");
        Append("Installation APK réussie.");
    }
    private string PackageName()
    {
        var value = package.Text.Trim();
        if (!Regex.IsMatch(value, @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)+$"))
            throw new InvalidOperationException("Identifiant de package invalide.");
        return value;
    }
    private async Task CheckPackage()
    {
        var r = await Adb("-s", RequireDevice(), "shell", "pm", "list", "packages", PackageName());
        Append(r.Output.Length == 0 ? "Application non trouvée." : r.Output);
    }
    private async Task LaunchPackage()
    {
        var r = await Adb("-s", RequireDevice(), "shell", "monkey", "-p", PackageName(), "-c", "android.intent.category.LAUNCHER", "1");
        Append(r.Output);
        if (r.ExitCode != 0) throw new InvalidOperationException("Impossible d'ouvrir cette application.");
    }
    private async Task ApplyPreset()
    {
        var endpoint = RequireDevice();
        var choice = preset.SelectedItem?.ToString() ?? "";
        string[] commands = choice switch
        {
            "Écran : veille 30 minutes" => new[] { "shell", "settings", "put", "system", "screen_off_timeout", "1800000" },
            "Écran : veille 5 minutes" => new[] { "shell", "settings", "put", "system", "screen_off_timeout", "300000" },
            "Animations : désactiver" => new[] { "shell", "settings", "put", "global", "window_animation_scale", "0" },
            "Animations : rétablir" => new[] { "shell", "settings", "put", "global", "window_animation_scale", "1" },
            "Rotation : automatique" => new[] { "shell", "settings", "put", "system", "accelerometer_rotation", "1" },
            "Rotation : verrouiller" => new[] { "shell", "settings", "put", "system", "accelerometer_rotation", "0" },
            "Accueil : touche HOME" => new[] { "shell", "input", "keyevent", "3" },
            "Redémarrer l'appareil" => new[] { "reboot" },
            _ => throw new InvalidOperationException("Choix inconnu.")
        };
        if (MessageBox.Show(this, $"Appliquer « {choice} » sur {endpoint} ?", "Confirmer le réglage", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var r = await Adb(new[] { "-s", endpoint }.Concat(commands).ToArray());
        Append($"{choice} : {(r.ExitCode == 0 ? "commande envoyée" : "échec")} {r.Output}");
        if (r.ExitCode != 0) throw new InvalidOperationException("Réglage refusé par l'appareil.");
        if (choice == "Redémarrer l'appareil") { selectedDevice = null; status.Text = "Redémarrage en cours"; }
    }
}
