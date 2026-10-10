using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace PushADB;

internal sealed partial class MainForm
{
    private sealed record Tweak(string Category, string Name, string Namespace, string Key, string Value, string Detail);
    private readonly ComboBox tweakCategory = new() { Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox tweakChoice = new() { Width = 305, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label tweakDescription = new() { AutoSize = true, MaximumSize = new Size(890, 0), ForeColor = Color.Silver };
    private readonly Dictionary<string, (string? Previous, string Applied)> tweakBackups = new();
    private static readonly Tweak[] Tweaks =
    {
        new("Écran", "Veille : 1 minute", "system", "screen_off_timeout", "60000", "Durée avant extinction de l'écran. Certains téléviseurs l'ignorent."),
        new("Écran", "Veille : 5 minutes", "system", "screen_off_timeout", "300000", "Durée avant extinction de l'écran."),
        new("Écran", "Veille : 15 minutes", "system", "screen_off_timeout", "900000", "Durée avant extinction de l'écran."),
        new("Écran", "Veille : 30 minutes", "system", "screen_off_timeout", "1800000", "Durée avant extinction de l'écran."),
        new("Écran", "Veille : 60 minutes", "system", "screen_off_timeout", "3600000", "Durée avant extinction de l'écran."),
        new("Écran", "Luminosité : 25 %", "system", "screen_brightness", "64", "Peut être sans effet sur les téléviseurs."),
        new("Écran", "Luminosité : 50 %", "system", "screen_brightness", "128", "Peut être sans effet sur les téléviseurs."),
        new("Écran", "Luminosité : 100 %", "system", "screen_brightness", "255", "Peut être sans effet sur les téléviseurs."),
        new("Écran", "Rotation : automatique", "system", "accelerometer_rotation", "1", "Souvent non applicable aux appareils TV."),
        new("Écran", "Rotation : verrouillée", "system", "accelerometer_rotation", "0", "Souvent non applicable aux appareils TV."),
        new("Écran", "Mise en veille pendant charge : normale", "global", "stay_on_while_plugged_in", "0", "Rétablit la mise en veille normale."),
        new("Écran", "Rester éveillé (alimentation secteur)", "global", "stay_on_while_plugged_in", "1", "À utiliser pour un diagnostic temporaire; peut augmenter la température."),
        new("Interface", "Animation fenêtres : 0", "global", "window_animation_scale", "0", "Désactive l'animation des fenêtres."),
        new("Interface", "Animation fenêtres : 0,5", "global", "window_animation_scale", "0.5", "Accélère l'animation des fenêtres."),
        new("Interface", "Animation fenêtres : 1", "global", "window_animation_scale", "1", "Valeur Android standard."),
        new("Interface", "Animation transitions : 0", "global", "transition_animation_scale", "0", "Désactive les transitions."),
        new("Interface", "Animation transitions : 0,5", "global", "transition_animation_scale", "0.5", "Accélère les transitions."),
        new("Interface", "Animation transitions : 1", "global", "transition_animation_scale", "1", "Valeur Android standard."),
        new("Interface", "Animateur : 0", "global", "animator_duration_scale", "0", "Désactive les animations de durée."),
        new("Interface", "Animateur : 0,5", "global", "animator_duration_scale", "0.5", "Accélère les animations."),
        new("Interface", "Animateur : 1", "global", "animator_duration_scale", "1", "Valeur Android standard."),
        new("Interface", "Touches sonores : désactiver", "system", "sound_effects_enabled", "0", "Désactive les effets sonores de navigation si pris en charge."),
        new("Interface", "Touches sonores : activer", "system", "sound_effects_enabled", "1", "Rétablit les effets sonores."),
        new("Interface", "Retour haptique : désactiver", "system", "haptic_feedback_enabled", "0", "Principalement pour les appareils mobiles."),
        new("Interface", "Retour haptique : activer", "system", "haptic_feedback_enabled", "1", "Principalement pour les appareils mobiles."),
        new("Accessibilité", "Texte : taille 85 %", "system", "font_scale", "0.85", "Change la taille des caractères système."),
        new("Accessibilité", "Texte : taille 100 %", "system", "font_scale", "1.0", "Taille des caractères normale."),
        new("Accessibilité", "Texte : taille 115 %", "system", "font_scale", "1.15", "Augmente la taille des caractères."),
        new("Accessibilité", "Texte : taille 130 %", "system", "font_scale", "1.3", "Augmente la taille des caractères."),
        new("Accessibilité", "Texte : taille 150 %", "system", "font_scale", "1.5", "Grand texte."),
        new("Audio", "Volume système : afficher en interface", "system", "volume_notification", "4", "Exemple de paramètre propre à certains appareils; peut être ignoré.")
    };

    private GroupBox BuildTweakPanel()
    {
        var box = Group("ANDROID TWEAKUI — RÉGLAGES CIBLÉS ET RÉVERSIBLES");
        var row = Row();
        tweakCategory.Items.AddRange(Tweaks.Select(t => t.Category).Distinct().Cast<object>().ToArray());
        tweakCategory.SelectedIndexChanged += (_, _) => RefreshTweakChoices();
        tweakCategory.SelectedIndex = 0;
        tweakChoice.SelectedIndexChanged += (_, _) =>
        {
            var tweak = CurrentTweak();
            tweakDescription.Text = tweak == null ? "" : tweak.Detail + "  [" + tweak.Namespace + "/" + tweak.Key + " = " + tweak.Value + "]";
        };
        row.Controls.Add(L("Catégorie"));
        row.Controls.Add(tweakCategory);
        row.Controls.Add(L("Réglage"));
        row.Controls.Add(tweakChoice);
        box.Controls.Add(row);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, WrapContents = false, AutoScroll = true };
        buttons.Controls.Add(B("Lire l'état", async () => await ReadTweak()));
        buttons.Controls.Add(B("Appliquer", async () => await ApplyTweak()));
        buttons.Controls.Add(B("Annuler dernière modification", async () => await UndoTweak()));
        buttons.Controls.Add(B("Inventaire Android", async () => await Inventory()));
        box.Controls.Add(buttons);
        tweakDescription.Dock = DockStyle.Bottom;
        tweakDescription.Height = 38;
        box.Controls.Add(tweakDescription);
        RefreshTweakChoices();
        return box;
    }

    private void RefreshTweakChoices()
    {
        var category = tweakCategory.SelectedItem?.ToString();
        tweakChoice.Items.Clear();
        foreach (var t in Tweaks.Where(x => x.Category == category)) tweakChoice.Items.Add(t.Name);
        if (tweakChoice.Items.Count > 0) tweakChoice.SelectedIndex = 0;
    }
    private Tweak? CurrentTweak() => Tweaks.FirstOrDefault(t => t.Category == tweakCategory.SelectedItem?.ToString() && t.Name == tweakChoice.SelectedItem?.ToString());

    private async Task ReadTweak()
    {
        var tweak = CurrentTweak() ?? throw new InvalidOperationException("Choisis un réglage.");
        var r = await Adb("-s", RequireDevice(), "shell", "settings", "get", tweak.Namespace, tweak.Key);
        Append($"TweakUI [{tweak.Namespace}/{tweak.Key}] : {r.Output}");
        if (r.ExitCode != 0) throw new InvalidOperationException("Lecture refusée par l'appareil.");
    }
    private async Task ApplyTweak()
    {
        var tweak = CurrentTweak() ?? throw new InvalidOperationException("Choisis un réglage.");
        var endpoint = RequireDevice();
        var before = await Adb("-s", endpoint, "shell", "settings", "get", tweak.Namespace, tweak.Key);
        if (before.ExitCode != 0) throw new InvalidOperationException("Impossible de sauvegarder la valeur existante.");
        var previous = before.Output.Trim();
        if (MessageBox.Show(this,
            $"Appareil : {endpoint}\nRéglage : {tweak.Name}\nValeur actuelle : {previous}\nNouvelle valeur : {tweak.Value}\n\n{tweak.Detail}\n\nAppliquer ?",
            "PushADB — Confirmer le changement", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var r = await Adb("-s", endpoint, "shell", "settings", "put", tweak.Namespace, tweak.Key, tweak.Value);
        if (r.ExitCode != 0 || r.Output.Contains("Permission denial", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Paramètre refusé : " + r.Output);
        tweakBackups[endpoint + "/" + tweak.Namespace + "/" + tweak.Key] = (previous == "null" ? null : previous, tweak.Value);
        var check = await Adb("-s", endpoint, "shell", "settings", "get", tweak.Namespace, tweak.Key);
        Append($"TweakUI : {tweak.Name}; avant={previous}; après={check.Output.Trim()}");
        if (check.Output.Trim() != tweak.Value)
            Append("Attention : la valeur n'est pas confirmée. L'appareil peut bloquer ce paramètre.");
    }
    private async Task UndoTweak()
    {
        var tweak = CurrentTweak() ?? throw new InvalidOperationException("Choisis un réglage.");
        var endpoint = RequireDevice();
        var id = endpoint + "/" + tweak.Namespace + "/" + tweak.Key;
        if (!tweakBackups.TryGetValue(id, out var backup))
        {
            Append("Aucune ancienne valeur enregistrée pour ce paramètre dans cette session.");
            return;
        }
        if (MessageBox.Show(this, "Rétablir l'ancienne valeur pour " + tweak.Key + " ?", "Annuler", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        string[] args = backup.Previous == null ?
            new[] { "-s", endpoint, "shell", "settings", "delete", tweak.Namespace, tweak.Key } :
            new[] { "-s", endpoint, "shell", "settings", "put", tweak.Namespace, tweak.Key, backup.Previous };
        var r = await Adb(args);
        if (r.ExitCode != 0) throw new InvalidOperationException("Restauration refusée : " + r.Output);
        tweakBackups.Remove(id);
        Append("Valeur précédente restaurée pour " + tweak.Namespace + "/" + tweak.Key);
    }
    private async Task Inventory()
    {
        var endpoint = RequireDevice();
        Append("=== INVENTAIRE " + endpoint + " ===");
        foreach (var args in new[] {
            new[] { "shell", "getprop", "ro.product.model" },
            new[] { "shell", "getprop", "ro.build.version.release" },
            new[] { "shell", "wm", "size" },
            new[] { "shell", "wm", "density" },
            new[] { "shell", "dumpsys", "display" },
            new[] { "shell", "dumpsys", "audio" },
            new[] { "shell", "df", "-h" }
        })
        {
            var r = await Adb(new[] { "-s", endpoint }.Concat(args).ToArray());
            Append(string.Join(" ", args) + " : " + (r.Output.Length > 2000 ? r.Output[..2000] + " [tronqué]" : r.Output));
        }
    }
}
