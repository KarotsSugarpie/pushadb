# PushADB v0.1.0 — Preview / Préversion

**Status: experimental preview, not a production-ready release.**

## Linux (Ubuntu / Debian)
New native Python 3 + Tkinter desktop edition:
- Authorized ADB connection by IPv4 and TCP port
- Device information and Android version
- Local APK install/update with confirmation
- Read/apply/undo selected reversible Android settings
- IPTV reboot, display, network, audio and crash diagnostics
- Export diagnostic logs as text

Install prerequisites:
```bash
sudo apt update
sudo apt install -y python3 python3-tk adb
```
Extract `PushADB-Linux-Preview.tar.gz` and run `./start.sh` inside `PushADB-Linux`.

## Windows
Windows .NET 8 codebase includes APK catalog, TweakUI, ADB console and IPTV monitoring. Windows standalone builds are provided by `build-local.ps1` when the SDK and build dependencies are installed. **No Windows binary is attached by this Linux release workflow.**

## Known limitations
- Authorize ADB on the target device; no security bypass.
- No Vega OS support.
- The Linux edition is a separate preview with fewer features than Windows.
- Full firmware and hardware testing not completed.
- Android/Fire OS can ignore or block device-specific settings.
- Closing or losing the streaming device connection does not alone prove a full reboot.
- Do not share raw device logs without checking sensitive contents.

---

**Français :** Première préversion Linux pour Ubuntu/Debian avec interface graphique, connexion ADB par IP, installation d'APK, ajustements réversibles et diagnostics IPTV. Nécessite `python3`, `python3-tk` et `adb`. Les applications et correctifs ne sont pas encore tous validés sur du matériel réel.
