# PushADB — Android TV & Fire TV Device Manager

**English** · [Français](#français)

> A Windows desktop toolkit for managing Android-based streaming devices through authorized Android Debug Bridge (ADB) connections.

PushADB brings together device discovery by IP, APK installation, application inspection, configurable Android settings, diagnostic reports, and an IPTV stability monitor. It is designed for Fire TV devices running Android-based Fire OS, Chromecast with Google TV, Google TV Streamer, and compatible Android TV boxes.

**Project status: active development / preview.** The code has been committed, but successful builds and comprehensive hardware testing have not yet been confirmed. Features may be incomplete or behave differently across firmware versions. Do not treat this as a finished release.

## Highlights

| Area | What PushADB provides |
| --- | --- |
| **Connections** | IPv4 address and adjustable TCP port, targeted connection/disconnection, device state including unauthorized/offline |
| **Device details** | Device model, Android version, basic inventory |
| **APK management** | Select local APK; install or update with ADB; verify package presence; attempt app launch |
| **App catalog** | Curated popular apps, official publisher pages, direct APK download only where a configured URL is available; optional follow-up installation |
| **Android TweakUI** | Screen timeout, brightness where supported, rotation, animation speeds, font size, selected interface settings |
| **Reversible changes** | Read setting first, confirm before applying, read back result, restore a previously saved value during the current app session |
| **ADB console** | Allowlisted read-only diagnostic commands, operation log, TXT export |
| **IPTV diagnostics** | Memory, network, thermal information, reboot indicators, codec and crash logs |
| **Live IPTV monitoring** | Periodic connection checks, reconnect attempts, boot identifier comparison, diagnostic collection after reconnection |
| **Maintenance** | Explicit reboot and HOME action, basic inventory and troubleshooting utilities |

## Screens and workflows

### 1. Connect to a device
1. Enable ADB debugging or wireless debugging in the streaming device's developer settings.
2. Enter its IPv4 address and port (commonly **5555** for network ADB on Fire OS; wireless pairing can use other ports).
3. Select **Connecter** and approve the ADB authorization prompt on the TV.
4. Confirm the device is shown as **device** rather than **unauthorized** or **offline**.

PushADB scopes device actions to the specified address and port. An authorized connection is mandatory. It does not bypass account registration, debug authorization, network restrictions, or device security.

### 2. Install or update an APK
Choose a local `.apk` file with **Parcourir...**, then use **Installer / Mettre à jour**. The application uses ADB to install onto the targeted device. Android may reject incompatible package architectures, signatures, system versions, or downgrade attempts. Using `-r` usually retains existing application data for a compatible update.

### 3. Application catalog
The initial catalog includes **TiviMate, VLC, Kodi, SmartTube, F-Droid, Downloader by AFTVnews, Send Files to TV, Jellyfin, Plex, YouTube, Netflix, Disney+ and Spotify**.

- Where a configured direct publisher APK URL exists, PushADB attempts to download the file locally, checks for a ZIP/APK header, and can offer to install it after confirmation.
- When no verified direct APK URL is configured, it opens the publisher's official page for manual selection.
- Apps that require a certified store or DRM-enabled distribution should be installed through the device's approved storefront.
- URLs and available versions change: verify the file and source before installing. An APK header check does **not** authenticate the publisher or cryptographic signature.
- The current catalog does not yet automatically select CPU architecture or universally support split APK bundles.

APK downloads are saved to `%LOCALAPPDATA%\PushADB\APKs`.

### 4. Android TweakUI
A collection of presets covers sleep timeout, screen brightness on supported hardware, rotation, animation scales, accessibility font size, interface sounds, and other limited settings. Every device and firmware is different: some commands may be refused, accepted without a visible effect, or reverted by the operating system.

For supported presets, PushADB reads the existing setting, requests confirmation, sends the change and reads back the value. The undo value is **in memory only** and disappears when the program closes. It is not a complete system backup.

### 5. IPTV Stability Doctor
Use this when a device freezes, drops the IPTV player, or **fully reboots** during playback.

- Collect device uptime, last boot reason when provided, recent process exit history, memory and storage observations.
- Inspect thermal service data and decoder-related logs when available.
- Monitor the ADB connection periodically while playing the problematic stream.
- After a reconnection, attempt to gather logs and compare boot identifiers.
- Export the collected text log for further analysis.

**Important:** Losing ADB connectivity alone does not prove a reboot. A Wi-Fi disruption can look identical. A complete power loss can leave no useful logs. If you see the Google boot logo, also test a known-good power adapter/cable and appropriate ventilation. Software settings cannot repair defective power hardware.

### 6. Diagnostic console
The preview console deliberately accepts only an approved list of diagnostic commands, not an unrestricted interactive shell. This keeps routine operation relatively safe. The log may include IP addresses, installed packages and device details; review it before sharing.

## Compatibility and limitations

| Device family | Current approach |
| --- | --- |
| Android-based **Amazon Fire OS** | Network ADB when developer settings and authentication permit |
| **Chromecast with Google TV** | Android ADB; may require wireless pairing on supported firmware |
| **Google TV Streamer** | Android ADB, pairing/port details depend on the OS |
| Generic **Android TV boxes** | Android ADB, with manufacturer-specific restrictions |
| **Vega OS Fire TV devices** | **Not supported by this ADB backend**; requires separate tooling |

No root access is required for ordinary ADB operations, but privileged settings, DRM controls, bootloader operations and firmware modification are **not** provided. PushADB is not an IPTV streaming service and does not include playlists or channels.

## Requirements

- Windows 10 or 11, x64.
- One authorized Android-based streaming device reachable over the network.
- For local builds: **.NET 8 SDK**, **Git**, and Internet access to NuGet and Google's platform-tools download.
- The published executable is intended to be self-contained; the ZIP also bundles Google's official ADB platform tools.

## Build on Windows

```powershell
git clone https://github.com/KarotsSugarpie/pushadb.git
cd pushadb
powershell -ExecutionPolicy Bypass -File .\build-local.ps1
```

The script restores the Windows x64 runtime dependencies, publishes a self-contained executable, fetches official Google platform-tools, and creates `PushADB-Windows-x64.zip` in the repository folder.

The runnable file is expected at `dist\PushADB.exe` **if compilation succeeds**. Build success has not been confirmed here; inspect and address compiler output before publishing a release.

Alternatively inspect [GitHub Actions](../../actions) for a successful run and download its ZIP artifact. The current workflow needs a runner labeled `self-hosted`, `Windows`, `X64`. Queued runs do not produce downloadable artifacts.

## Safety and privacy

Use PushADB only on devices you own or are permitted to administer. Never enable network debugging on untrusted networks. Check each action and review changes before applying. Logs are stored locally in the app's user-accessible UI or exported to your chosen destination; APK files are saved locally. No claims are made about security auditing or production readiness.

## Roadmap (not yet delivered)

- Multi-device dashboard and saved connection profiles.
- Wireless ADB pairing-code wizard.
- Verified app download metadata, checksums and ABI auto-selection.
- Split APK support.
- More reliable persistent backups, preview/diff and rollback.
- Separate tabbed pages for audio, video, networking, apps, diagnostics and expert commands.
- Automated build verification, signed releases, installer and more thorough device testing.

## Contributing

Bug reports and contributions are welcome once the project is public. Include device model, Android/Fire OS version, steps to reproduce and sanitized logs. Never publish private IPs, authentication tokens, credentials or full unredacted diagnostic dumps.

---

# Français

> **PushADB** est une application Windows de gestion des appareils de diffusion Android utilisant une connexion Android Debug Bridge (ADB) autorisée.

Elle rassemble dans un seul outil la connexion par IP, l'installation d'APK, un catalogue d'applications, un panneau de réglages façon **TweakUI**, une console de diagnostic et la surveillance des problèmes IPTV. Elle vise les Fire TV avec Fire OS fondé sur Android, Chromecast avec Google TV, Google TV Streamer et boîtiers Android TV compatibles.

**État du projet : développement actif / préversion.** Le code est publié dans le dépôt, mais la compilation finale et les essais sur différents modèles ne sont pas encore confirmés. Certaines fonctions peuvent demander des ajustements.

## Fonctionnalités

| Module | Fonctionnement |
| --- | --- |
| **Connexion ADB** | Saisie de l'IP et du port, connexion, déconnexion, états autorisé / non autorisé / hors ligne |
| **Informations appareil** | Modèle, version Android, inventaire de base |
| **Gestion APK** | Sélection, installation, mise à jour et vérification d'application |
| **Catalogue** | Applications populaires et sources officielles, téléchargement direct lorsque configuré |
| **Android TweakUI** | Veille, luminosité compatible, rotation, animations, taille du texte, réglages d'interface |
| **Validation des réglages** | Lecture avant changement, confirmation, contrôle après changement, annulation pendant la session |
| **Console ADB** | Diagnostics en lecture seule et journal exportable |
| **Doctor IPTV** | Mémoire, température disponible, réseau, erreurs vidéo, redémarrages |
| **Surveillance IPTV** | Détection des coupures ADB, tentative de reconnexion et collecte après redémarrage |

### Connexion à un Fire TV ou Google TV

1. Activez le **débogage ADB** ou le débogage sans fil dans les options pour développeurs de l'appareil.
2. Saisissez son adresse IPv4 et son port.
3. Cliquez sur **Connecter**.
4. Autorisez l'ordinateur à partir de l'écran du téléviseur.
5. Vérifiez que l'état ADB affiche `device`.

L'application ne contourne ni les demandes d'autorisation ni l'inscription aux comptes du fabricant.

### Installation et catalogue d'applications

Choisissez un APK sur l'ordinateur ou utilisez le catalogue. Les applications initiales comprennent **TiviMate, VLC, Kodi, SmartTube, F-Droid, Downloader, Send Files to TV, Jellyfin, Plex, YouTube, Netflix, Disney+ et Spotify**.

Lorsqu'un téléchargement APK direct est disponible dans le catalogue, PushADB récupère le fichier localement, contrôle son format de base, puis propose de l'installer sur l'appareil sélectionné. Sinon, il ouvre la page officielle de l'éditeur. Pour certaines applications, la boutique officielle de l'appareil reste indispensable.

L'option de mise à jour `-r` permet normalement de conserver les données si Android accepte la nouvelle version et sa signature. La sélection automatique de l'architecture et les ensembles d'APK fractionnés ne sont pas encore complètement gérés.

### Android TweakUI

L'outil offre des préréglages de durée de veille, luminosité sur matériel compatible, vitesse des animations, rotation, taille du texte et divers paramètres d'interface. Avant un changement, il lit la valeur actuelle et affiche une confirmation. Après l'opération, il essaie de vérifier la nouvelle valeur.

**Attention :** Android TV et Fire OS n'acceptent pas toutes les commandes ADB. La fonction d'annulation mémorise la valeur précédente pendant la session uniquement; elle ne remplace pas une sauvegarde complète.

### Diagnostic de redémarrage IPTV

Si un Chromecast redémarre entièrement avec le logo Google, PushADB peut aider à rechercher des indices dans les journaux, l'état thermique, l'historique de démarrage et les statistiques mémoire. La surveillance tente de détecter une déconnexion, de se reconnecter et de collecter des informations après le retour de l'appareil.

Une coupure ADB ne confirme **pas** à elle seule un redémarrage complet. Une panne d'alimentation peut aussi ne laisser aucun journal. Vérifiez l'adaptateur secteur, le câble USB-C et la ventilation avant de modifier des paramètres sensibles.

### Console et diagnostic

La console intégrée accepte pour l'instant une liste prédéfinie de commandes de diagnostic en lecture seule. Le journal exportable facilite le dépannage, tout en évitant l'exécution accidentelle de commandes système dangereuses.

## Compatibilité

- **Fire TV Stick / Fire TV sous Fire OS Android :** ADB autorisé, selon les restrictions Amazon.
- **Chromecast avec Google TV / Google TV Streamer :** ADB autorisé; certains modèles nécessitent l'association sans fil avec code.
- **Autres boîtiers Android TV :** fonctionnement dépendant du fabricant et de la version.
- **Fire TV sous Vega OS :** non pris en charge par le moteur ADB Android; une autre intégration serait nécessaire.

PushADB ne fournit ni abonnement IPTV, ni chaînes, ni contournement DRM, ni rootage automatique.

## Installation et compilation

Sur Windows avec le SDK .NET 8 et Git :

```powershell
git clone https://github.com/KarotsSugarpie/pushadb.git
cd pushadb
powershell -ExecutionPolicy Bypass -File .\build-local.ps1
```

Si la compilation se termine correctement, l'application sera dans `dist\PushADB.exe` et l'archive `PushADB-Windows-x64.zip` sera créée à la racine.

Vous pouvez aussi consulter l'onglet [Actions](../../actions). Le workflow actuel réclame un runner Windows auto-hébergé muni des étiquettes `self-hosted`, `Windows` et `X64`. Un build en attente n'est pas une release validée.

## Sécurité et confidentialité

Utilisez exclusivement des appareils que vous possédez ou administrez avec autorisation. Ne laissez pas le débogage ADB ouvert sur un réseau non fiable. Les journaux peuvent révéler des adresses IP, des noms d'applications ou d'autres informations privées; relisez-les avant publication. Les APK téléchargés sont conservés localement.

## Évolutions prévues

Profils multiappareils, association sans fil par code, téléchargements APK mieux authentifiés, détection automatique ARM 32/64 bits, APK fractionnés, sauvegarde/restauration persistante, onglets spécialisés, compilations automatisées validées, signatures et installateur Windows.

## Participation

Les signalements de bogues, propositions et contributions sont les bienvenus. Précisez le modèle, la version Android ou Fire OS, la procédure pour reproduire le problème et joignez des journaux anonymisés.

---

**PushADB** — Windows ADB management and diagnostics for supported streaming devices.

## Language / Langue

The Windows interface now starts in **English** on first launch. Use the **Language / Langue** selector at the top right to switch between **English** and **Français**. Your choice is saved to `%LOCALAPPDATA%\\PushADB\\language.txt` and reloaded on the next start. The main interface and common choice lists are translated; some diagnostic details, device-returned ADB messages and advanced preset descriptions may still appear in their original language during this preview.

L'interface démarre en **anglais** au premier lancement. Le sélecteur **Language / Langue** en haut à droite permet de passer au **français**. Le choix est conservé au prochain démarrage. Certains détails de diagnostic et descriptions avancées restent à traduire dans cette préversion.

## Linux preview / Préversion Linux

A native Linux GUI has been added in `linux/pushadb.py` using Python 3 and Tkinter. Linux functions currently cover connection by IPv4 + port, APK installation, basic TweakUI with session rollback, Android diagnostics and log export. Linux does **not** yet include every Windows catalog/monitoring feature.

**Ubuntu / Debian installation:**
```bash
sudo apt update
sudo apt install -y python3 python3-tk adb
git clone https://github.com/KarotsSugarpie/pushadb.git
cd pushadb
python3 linux/pushadb.py
```
Or run `bash build-linux.sh`, extract `PushADB-Linux-Preview.tar.gz`, and launch `PushADB-Linux/start.sh`.

**GitHub builds:** [Linux artifact workflow](../../actions/workflows/linux-build.yml). The separate [Publish PushADB Linux Preview Release](../../actions/workflows/publish-preview.yml) workflow can be manually triggered with a new preview tag. Publication of a GitHub **Release** is distinct from the existing Actions build artifact.

**Français :** L'édition Linux native utilise Python 3, Tkinter et ADB. Elle peut se lancer sur Ubuntu/Debian après installation de `python3`, `python3-tk` et `adb`. La préversion Linux propose les fonctions essentielles mais ne possède pas encore tout le catalogue avancé de Windows.
