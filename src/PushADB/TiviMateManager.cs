using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PushADB;

internal sealed partial class MainForm
{
    private Task OpenTiviMateManager()
    {
        var endpoint = RequireDevice();
        using var dialog = new TiviMateManagerDialog(endpoint, args => Adb(args), Append, french);
        dialog.ShowDialog(this);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Guided TiviMate administration for authorized Android ADB devices.
/// No private application storage access, rooting, undocumented TiviMate API,
/// or hard-coded screen coordinates are required.
/// </summary>
internal sealed class TiviMateManagerDialog : Form
{
    private const string PackageId = "ar.tvplayer.tv";
    private readonly string endpoint;
    private readonly Func<string[], Task<(int ExitCode, string Output)>> adb;
    private readonly Action<string> log;
    private readonly bool french;
    private readonly TextBox server = new() { Width = 770 };
    private readonly TextBox user = new() { Width = 770 };
    private readonly TextBox password = new() { Width = 770, UseSystemPasswordChar = true };
    private readonly TextBox playlist = new() { Width = 770 };
    private readonly TextBox epg = new() { Width = 770 };
    private readonly ComboBox targetField = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox remoteFiles = new() { Width = 760, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label detection = new() { AutoSize = true, ForeColor = Color.SteelBlue };
    private readonly System.Collections.Generic.List<Button> buttons = new();
    private bool working;

    private sealed record DeviceBackup(string Path)
    {
        public override string ToString() => Path;
    }

    internal TiviMateManagerDialog(
        string endpoint,
        Func<string[], Task<(int ExitCode, string Output)>> adb,
        Action<string> log,
        bool french)
    {
        this.endpoint = endpoint;
        this.adb = adb;
        this.log = log;
        this.french = french;

        Text = T("PushADB — Gestion TiviMate", "PushADB — TiviMate Manager");
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(24, 33, 48);
        ForeColor = Color.WhiteSmoke;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 580);
        Size = new Size(930, 740);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildConfiguration());
        tabs.TabPages.Add(BuildBackups());
        tabs.TabPages.Add(BuildRemote());
        Controls.Add(tabs);

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 74, Padding = new Padding(12, 7, 12, 6),
            WrapContents = true, AutoScroll = true
        };
        top.Controls.Add(TextLabel(T("Appareil : ", "Device: ") + endpoint, 205));
        top.Controls.Add(Action(T("Détecter TiviMate", "Detect TiviMate"), Detect));
        top.Controls.Add(Action(T("Ouvrir TiviMate", "Open TiviMate"), Launch));
        top.Controls.Add(detection);
        Controls.Add(top);
        FormClosing += (_, _) =>
        {
            password.Clear(); // No credentials are persisted to disk by this module.
        };
    }

    private string T(string fr, string en) => french ? fr : en;

    private static Label TextLabel(string text, int width = 840) => new()
    {
        Text = text, AutoSize = false, Width = width, Height = 40,
        TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(5)
    };

    private static FlowLayoutPanel Page() => new()
    {
        Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false,
        FlowDirection = FlowDirection.TopDown, Padding = new Padding(13)
    };

    private static TabPage Tab(string name, Control body)
    {
        var page = new TabPage(name) { BackColor = Color.FromArgb(24, 33, 48), ForeColor = Color.WhiteSmoke };
        page.Controls.Add(body);
        return page;
    }

    private Button Action(string label, Func<Task> operation)
    {
        var button = new Button
        {
            Text = label, AutoSize = true, MinimumSize = new Size(126, 36),
            Margin = new Padding(5), FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(33, 79, 111), ForeColor = Color.WhiteSmoke
        };
        buttons.Add(button);
        button.Click += async (_, _) => await Execute(operation);
        return button;
    }

    private async Task Execute(Func<Task> operation)
    {
        if (working) return;
        working = true;
        foreach (var button in buttons) button.Enabled = false;
        try { await operation(); }
        catch (Exception ex)
        {
            // Never attach raw ADB output here: the active input may be a password.
            log("TiviMate : échec d'une opération (détails affichés localement).");
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            working = false;
            if (!IsDisposed)
                foreach (var button in buttons) button.Enabled = true;
        }
    }

    private void AddField(FlowLayoutPanel page, string label, TextBox input)
    {
        page.Controls.Add(TextLabel(label, 810));
        input.Margin = new Padding(5, 0, 5, 9);
        page.Controls.Add(input);
    }

    private TabPage BuildConfiguration()
    {
        var page = Page();
        page.Controls.Add(TextLabel(T(
            "Configuration assistée : navigue jusqu'au champ souhaité sur la TV, sélectionne son type ci-dessous, puis envoie sa valeur. Aucun menu n'est deviné automatiquement.",
            "Guided setup: navigate to a field on the TV, select its type below, then send its value. Menu navigation is not guessed automatically."), 835));

        AddField(page, T("Adresse serveur Xtream (URL et port)", "Xtream server URL and port"), server);
        AddField(page, T("Utilisateur Xtream", "Xtream username"), user);
        AddField(page, T("Mot de passe Xtream (jamais journalisé)", "Xtream password (never logged)"), password);
        AddField(page, T("Adresse playlist M3U", "M3U playlist URL"), playlist);
        AddField(page, T("Adresse EPG", "EPG URL"), epg);

        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Width = 840 };
        row.Controls.Add(TextLabel(T("Champ actuellement sélectionné sur la TV :", "Field currently selected on TV:"), 280));
        targetField.Items.AddRange(new object[] {
            T("Serveur Xtream", "Xtream server"),
            T("Utilisateur Xtream", "Xtream username"),
            T("Mot de passe Xtream", "Xtream password"),
            T("Playlist M3U", "M3U playlist"),
            T("URL EPG", "EPG URL")
        });
        targetField.SelectedIndex = 0;
        row.Controls.Add(targetField);
        row.Controls.Add(Action(T("Envoyer le champ", "Send selected field"), SendCurrentField));
        page.Controls.Add(row);

        page.Controls.Add(TextLabel(T(
            "Conseil : utilise l'onglet Télécommande pour choisir le champ sur la TV. La saisie ADB fonctionne surtout avec des caractères ASCII; les données ne sont ni sauvegardées ni ajoutées au journal.",
            "Tip: use the Remote tab to focus the TV field. ADB text input works best with ASCII. Credentials are not saved or added to logs."), 835));
        return Tab(T("Xtream / M3U / EPG", "Xtream / M3U / EPG"), page);
    }

    private TabPage BuildBackups()
    {
        var page = Page();
        page.Controls.Add(TextLabel(T(
            "IMPORT : sélectionner un fichier .tmb local et le copier dans Downloads sur la TV. Ensuite, dans TiviMate : Paramètres > Général > Restaurer, puis choisir ce fichier. La restauration n'est jamais déclenchée sans confirmation dans TiviMate.",
            "IMPORT: select a local .tmb and copy it to TV Downloads. Then in TiviMate: Settings > General > Restore, and choose the file. Restoration is never triggered without confirmation in TiviMate."), 835));
        page.Controls.Add(Action(T("Envoyer une sauvegarde .tmb vers la TV", "Copy .tmb backup to TV"), ImportBackup));

        page.Controls.Add(TextLabel(T(
            "EXPORT : crée d'abord une sauvegarde depuis TiviMate (Paramètres > Général > Sauvegarder) vers Download ou Documents, puis recherche et récupère le .tmb sur le PC.",
            "EXPORT: first create a backup in TiviMate (Settings > General > Backup) under Download or Documents, then discover and pull the .tmb to the PC."), 835));

        var row = new FlowLayoutPanel { Width = 850, AutoSize = true, WrapContents = true };
        row.Controls.Add(Action(T("Lister les sauvegardes .tmb", "Find .tmb backups"), FindBackups));
        row.Controls.Add(Action(T("Récupérer sur le PC", "Copy selected backup to PC"), ExportBackup));
        page.Controls.Add(row);
        page.Controls.Add(remoteFiles);
        page.Controls.Add(TextLabel(T(
            "Compatibilité : la version TiviMate, les comptes et Premium peuvent différer. La restauration peut remplacer les réglages existants. Conserve une sauvegarde préalable.",
            "Compatibility: TiviMate versions, accounts and Premium may differ. Restoring can replace existing settings. Keep an earlier backup."), 835));
        return Tab(T("Sauvegardes .tmb", ".tmb backups"), page);
    }

    private TabPage BuildRemote()
    {
        var page = Page();
        page.Controls.Add(TextLabel(T(
            "Télécommande ADB : navigue dans les menus de TiviMate manuellement. Les touches agissent sur l'application actuellement visible sur la TV.",
            "ADB remote: navigate TiviMate menus manually. Keys act on the app currently displayed on the TV."), 835));

        var grid = new TableLayoutPanel { ColumnCount = 3, RowCount = 4, Width = 530, Height = 228 };
        for (var i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        for (var i = 0; i < 4; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        void AddKey(int col, int row, string label, int key)
        {
            var b = Action(label, () => Key(key));
            b.Dock = DockStyle.Fill;
            grid.Controls.Add(b, col, row);
        }

        AddKey(1, 0, "↑", 19);
        AddKey(0, 1, "←", 21);
        AddKey(1, 1, "OK", 23);
        AddKey(2, 1, "→", 22);
        AddKey(1, 2, "↓", 20);
        AddKey(0, 3, T("Retour", "Back"), 4);
        AddKey(1, 3, "TAB", 61);
        AddKey(2, 3, T("Entrée", "Enter"), 66);
        page.Controls.Add(grid);
        page.Controls.Add(Action(T("Accueil Android (HOME)", "Android HOME"), () => Key(3)));
        page.Controls.Add(TextLabel(T(
            "Attention : certains champs TiviMate ou claviers virtuels n'acceptent pas toutes les touches ni la saisie injectée par ADB.",
            "Note: some TiviMate fields or onscreen keyboards may not accept every key or injected ADB text."), 835));
        return Tab(T("Télécommande", "Remote"), page);
    }

    private Task<(int ExitCode, string Output)> RunAdb(params string[] arguments) =>
        adb(new[] { "-s", endpoint }.Concat(arguments).ToArray());

    private async Task Detect()
    {
        var installed = await RunAdb("shell", "pm", "path", PackageId);
        if (installed.ExitCode != 0 || !installed.Output.Contains("package:", StringComparison.Ordinal))
        {
            detection.Text = T("TiviMate non détecté", "TiviMate not found");
            return;
        }
        var details = await RunAdb("shell", "dumpsys", "package", PackageId);
        var match = Regex.Match(details.Output, @"\bversionName=([^\s]+)");
        detection.Text = T("Installé", "Installed") +
            (match.Success ? " v" + match.Groups[1].Value : "");
        log("TiviMate : application détectée sur l'appareil.");
    }

    private async Task Launch()
    {
        var result = await RunAdb("shell", "monkey", "-p", PackageId,
            "-c", "android.intent.category.LAUNCHER", "1");
        if (result.ExitCode != 0 || result.Output.Contains("No activities found", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(T("Impossible de lancer TiviMate.", "Unable to launch TiviMate."));
        log("TiviMate : demande de lancement envoyée.");
    }

    private async Task Key(int code)
    {
        var result = await RunAdb("shell", "input", "keyevent", code.ToString());
        if (result.ExitCode != 0)
            throw new InvalidOperationException(T("Touche ADB refusée par la TV.", "ADB key rejected by TV."));
    }

    private async Task SendCurrentField()
    {
        var value = targetField.SelectedIndex switch
        {
            0 => server.Text,
            1 => user.Text,
            2 => password.Text,
            3 => playlist.Text,
            4 => epg.Text,
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(T("La valeur sélectionnée est vide.", "Selected value is empty."));

        // input text is not a Unicode keyboard: literal "%s" becomes a space in Android.
        if (value.Any(c => c < 32 || c > 126) || value.Contains("%s", StringComparison.Ordinal))
            throw new InvalidOperationException(T(
                "Saisie ADB non fiable pour ces caractères (Unicode ou %s). Saisis ce champ avec le clavier de la TV.",
                "ADB input cannot reliably type these characters (Unicode or %s). Enter this field with the TV keyboard."));

        var answer = MessageBox.Show(this, T(
            "Place d'abord le curseur DANS le bon champ de TiviMate sur la TV. La valeur sera tapée à cet emplacement.\nContinuer ?",
            "Focus the correct TiviMate text field on your TV FIRST. The value will be typed into the focused field.\nContinue?"),
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        // Quote a single remote shell argument. Never log the command/output:
        // ADB process arguments can expose credentials to local administrators.
        var shellValue = value.Replace(" ", "%s", StringComparison.Ordinal);
        var escaped = "'" + shellValue.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
        var result = await RunAdb("shell", "input text " + escaped);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(T("La saisie a été refusée. Utilise le clavier de la TV.", "Text input failed. Use the TV keyboard."));
        log("TiviMate : saisie ADB envoyée (contenu masqué).");
    }

    private async Task ImportBackup()
    {
        using var picker = new OpenFileDialog
        {
            Title = T("Choisir une sauvegarde TiviMate", "Select TiviMate backup"),
            Filter = "TiviMate (*.tmb)|*.tmb", CheckFileExists = true
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        var info = new FileInfo(picker.FileName);
        if (info.Length < 1 || info.Length > 1024L * 1024 * 1024 ||
            !string.Equals(info.Extension, ".tmb", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(T("Fichier .tmb invalide ou trop volumineux.", "Invalid or oversized .tmb file."));

        var remote = "/sdcard/Download/PushADB-TiviMate-" +
            DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8] + ".tmb";
        if (MessageBox.Show(this, T(
            "Copier ce .tmb dans le dossier Download de la TV ?\nTiviMate devra ensuite restaurer ce fichier manuellement.",
            "Copy this .tmb to TV Downloads?\nYou must then restore it manually inside TiviMate."),
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var result = await RunAdb("push", info.FullName, remote);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(T("Transfert ADB échoué.", "ADB upload failed."));
        log("TiviMate : sauvegarde .tmb transférée vers Downloads.");
        MessageBox.Show(this, T(
            "Fichier copié : " + remote + "\nDans TiviMate : Paramètres > Général > Restaurer. Choisis le fichier dans Download. Vérifie l'impact avant confirmation.",
            "File copied: " + remote + "\nIn TiviMate: Settings > General > Restore. Choose it from Downloads. Review impact before confirming."),
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task FindBackups()
    {
        remoteFiles.Items.Clear();
        foreach (var directory in new[] { "/sdcard/Download", "/sdcard/Documents" })
        {
            var result = await RunAdb("shell", "ls", "-1", directory);
            if (result.ExitCode != 0) continue;
            foreach (var line in result.Output.Replace("\r", "").Split('\n'))
            {
                var name = line.Trim();
                if (!name.EndsWith(".tmb", StringComparison.OrdinalIgnoreCase) ||
                    name.Length > 180 || name.Contains('/') || name.Contains('\\') ||
                    name == ".." || name.Any(char.IsControl)) continue;
                remoteFiles.Items.Add(new DeviceBackup(directory + "/" + name));
            }
        }
        if (remoteFiles.Items.Count > 0) remoteFiles.SelectedIndex = 0;
        log("TiviMate : " + remoteFiles.Items.Count + " sauvegarde(s) .tmb dans Downloads/Documents.");
        if (remoteFiles.Items.Count == 0)
            MessageBox.Show(this, T(
                "Aucun .tmb trouvé. Fais d'abord une sauvegarde depuis TiviMate dans Download ou Documents.",
                "No .tmb found. First back up in TiviMate to Download or Documents."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ExportBackup()
    {
        if (remoteFiles.SelectedItem is not DeviceBackup item)
            throw new InvalidOperationException(T("Liste et choisis un fichier .tmb d'abord.", "Find and select a .tmb first."));
        using var picker = new SaveFileDialog
        {
            Title = T("Sauvegarder sur l'ordinateur", "Save backup to computer"),
            Filter = "TiviMate (*.tmb)|*.tmb",
            FileName = System.IO.Path.GetFileName(item.Path),
            OverwritePrompt = true
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        var result = await RunAdb("pull", item.Path, picker.FileName);
        if (result.ExitCode != 0 || !File.Exists(picker.FileName) || new FileInfo(picker.FileName).Length == 0)
            throw new InvalidOperationException(T("La récupération ADB n'a pas pu être validée.", "ADB backup download could not be verified."));
        log("TiviMate : sauvegarde .tmb récupérée sur le PC.");
        MessageBox.Show(this, T("Sauvegarde copiée sur le PC.", "Backup copied to the PC."),
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
