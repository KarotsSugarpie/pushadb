using System.Drawing;
using System.Windows.Forms;

namespace PushADB;

internal sealed partial class MainForm
{
    private readonly ComboBox languageSelector = new() { Width = 116, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Dictionary<Control, string> originalCaptions = new();
    private bool french;
    private static readonly Dictionary<string,string> En = new(StringComparer.Ordinal)
    {
        ["Gestionnaire ADB pour Fire TV et Google / Android TV"]="ADB Manager for Fire TV and Google / Android TV",
        ["CONNEXION"]="CONNECTION",["Adresse IP"]="IP address",["Connecter"]="Connect",
        ["Déconnecter"]="Disconnect",["Actualiser"]="Refresh",
        ["APPLICATIONS / APK"]="APPLICATIONS / APK",["Fichier APK"]="APK file",
        ["Parcourir..."]="Browse...",["Installer / Mettre à jour"]="Install / Update",
        ["Identifiant package"]="Package ID",["Vérifier"]="Check",["Ouvrir"]="Launch",
        ["Infos appareil"]="Device details",["Gestion TiviMate"]="TiviMate Manager",["CONFIGURATION RAPIDE"]="QUICK SETTINGS",
        ["Appliquer"]="Apply",["Lister les appareils"]="List devices",
        ["CONSOLE ADB ET DIAGNOSTIC IPTV"]="ADB CONSOLE & IPTV DIAGNOSTICS",
        ["Analyser"]="Run diagnostics",["Exporter journal"]="Export log",
        ["Surveiller IPTV"]="Monitor IPTV",["Arrêter surveillance"]="Stop monitoring",
        ["Commande ADB"]="ADB command",["Exécuter"]="Run",
        ["JOURNAL DES OPÉRATIONS"]="OPERATION LOG",
        ["ANDROID TWEAKUI — RÉGLAGES CIBLÉS ET RÉVERSIBLES"]="ANDROID TWEAKUI — CONTROLLED & REVERSIBLE SETTINGS",
        ["Catégorie"]="Category",["Réglage"]="Setting",["Lire l'état"]="Read value",
        ["Annuler dernière modification"]="Undo last change",["Inventaire Android"]="Android inventory",
        ["CATALOGUE D'APPLICATIONS — SOURCE OFFICIELLE"]="APPLICATION CATALOG — OFFICIAL SOURCES",
        ["Application"]="Application",["Site officiel"]="Official website",
        ["Télécharger APK"]="Download APK",["Installer APK téléchargé"]="Install downloaded APK",
        ["Ouvrir dossier APK"]="Open APK folder",
        ["Installer après téléchargement"]="Install after download",
        ["Conserver les données (-r)"]="Keep app data (-r)",
        ["Non connecté"]="Disconnected",["Déconnecté"]="Disconnected",["Connecté"]="Connected",
        ["Autorisation requise"]="Authorization required",["Hors ligne"]="Offline",
        ["Appareil : aucun"]="Device: none",["Redémarrage en cours"]="Rebooting",
        ["Écran : veille 30 minutes"]="Screen: sleep after 30 minutes",
        ["Écran : veille 5 minutes"]="Screen: sleep after 5 minutes",
        ["Animations : désactiver"]="Animations: off",["Animations : rétablir"]="Animations: restore",
        ["Rotation : automatique"]="Rotation: auto",["Rotation : verrouiller"]="Rotation: lock",
        ["Accueil : touche HOME"]="Home key",["Redémarrer l'appareil"]="Reboot device",
        ["Diagnostic complet (lecture seule)"]="Full diagnostics (read-only)",
        ["Batterie / alimentation"]="Battery / power",["Mémoire et processus"]="Memory and processes",
        ["Températures et thermique"]="Temperature / thermal",
        ["Wi-Fi et réseau"]="Wi-Fi and network",
        ["Historique des redémarrages"]="Reboot history",
        ["Erreurs vidéo et plantages"]="Video errors and crashes",
        ["Écran"]="Display",["Interface"]="Interface",["Accessibilité"]="Accessibility",
        ["Audio"]="Audio",
        ["Les réglages disponibles dépendent de la version Android / Fire OS et des permissions ADB. Aucune action n'est lancée sans clic."]="Available settings depend on the Android / Fire OS version and ADB permissions. No action runs without a click."
    };

    private string LanguageFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PushADB", "language.txt");

    private void AddLanguageSwitcher(Control header)
    {
        var label = new Label { Text = "Language / Langue", AutoSize = true, ForeColor = Color.Silver, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var picker = languageSelector;
        picker.Items.AddRange(new object[] { "English", "Français" });
        picker.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        void Place()
        {
            label.Left = Math.Max(350, header.ClientSize.Width - 290);
            label.Top = 11;
            picker.Left = Math.Max(470, header.ClientSize.Width - 125);
            picker.Top = 6;
        }
        header.Controls.Add(label);
        header.Controls.Add(picker);
        header.Resize += (_, _) => Place();
        Place();
        picker.SelectedIndexChanged += (_, _) =>
        {
            french = picker.SelectedIndex == 1;
            ApplyLanguage();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LanguageFile)!);
                File.WriteAllText(LanguageFile, french ? "fr" : "en");
            }
            catch (Exception ex) { Append("Language preference: " + ex.Message); }
        };
    }

    private void InitializeLanguage()
    {
        CaptureOriginalCaptions(this);
        foreach (var combo in new[] { preset, diagnostic, tweakCategory, tweakChoice, catalogCategory, catalogApp })
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DrawItem += (_, e) =>
            {
                if (e.Index < 0 || e.Index >= combo.Items.Count) return;
                e.DrawBackground();
                var value = combo.Items[e.Index]?.ToString() ?? "";
                var shown = french ? value : Translate(value);
                TextRenderer.DrawText(e.Graphics, shown, combo.Font, e.Bounds, e.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.DrawFocusRectangle();
            };
        }
        var preferred = "en";
        try { if (File.Exists(LanguageFile)) preferred = File.ReadAllText(LanguageFile).Trim(); }
        catch { /* language remains English */ }
        languageSelector.SelectedIndex = preferred == "fr" ? 1 : 0;
    }

    private void CaptureOriginalCaptions(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Label or Button or CheckBox or GroupBox)
                originalCaptions[control] = control.Text;
            CaptureOriginalCaptions(control);
        }
    }

    private static string Translate(string original) => En.TryGetValue(original, out var english) ? english : original;

    private void ApplyLanguage()
    {
        foreach (var entry in originalCaptions)
            if (!entry.Key.IsDisposed)
                entry.Key.Text = french ? entry.Value : Translate(entry.Value);
        foreach (var combo in new[] { preset, diagnostic, tweakCategory, tweakChoice, catalogCategory, catalogApp })
            combo.Invalidate();
    }
}
