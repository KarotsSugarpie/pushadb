# PushADB — Windows Android TV / Fire TV Manager

Native Windows desktop application for managing **authorized** Fire TV, Fire Stick, Chromecast with Google TV and Android TV devices by IP using Android Debug Bridge.

## Download the Windows build

Open [GitHub Actions](https://github.com/KarotsSugarpie/pushadb/actions) → **Build PushADB Windows** → select a successful run → **Artifacts** → `PushADB-Windows-x64`. Extract the ZIP and run `PushADB.exe` from inside the extracted directory.

The workflow compiles with .NET 8 and bundles the **official Google Android Platform-Tools** in `platform-tools/`. The executable is self-contained and does not require a separate .NET installation. An artifact is **not a GitHub Release**; the workflow does not publish tagged releases automatically.

## Features

- Connect or disconnect an IPv4 address with a configurable ADB port (default 5555).
- Show authorization state: connected / unauthorized / offline.
- Display device model and Android version.
- Browse to an APK on your Windows computer and install or update it using `adb install -r`.
- Check whether a package is installed, and launch its main activity when supported.
- Apply explicit, confirmed presets: display timeout, window animations, rotation setting, HOME button, or restart.
- All changes target the selected IP:port with `adb -s`; other connected devices are not selected accidentally.
- Logs are shown in the application; long operations have a five-minute timeout.

## First connection

1. Enable **ADB debugging** in your Fire TV or Android TV developer settings.
2. Type the device IPv4 address (example `192.168.254.161`) and click **Connecter**.
3. On the device, accept the computer's ADB RSA authorization prompt.
4. If the application says *Autorisation requise*, authorize the connection and click **Actualiser**.
5. Click **Parcourir...**, choose a trusted `.apk` file, and click **Installer / Mettre à jour**.

Do not enter a package URL in the APK field: this version installs a local APK chosen in the file picker. The app does not automatically download third-party APKs.

## Important limitations

- ADB must be enabled and the connection must be **authorized on the device**. The tool does not bypass the pairing prompt.
- Settings behavior depends on device vendor, Android/Fire OS version, and privileges.
- Updating an app with `-r` normally retains its data when Android accepts the update, but signature/version incompatibilities can prevent it.
- The app does **not** remove data, uninstall apps or disable system security features.
- Port 5555 is typical for legacy network ADB; some Google TV devices require **Wireless debugging pairing** and a dynamic port. This version does not yet have a pairing-code wizard.
- Treat APKs as untrusted unless you know the publisher.
- Use only on devices you own or have explicit authorization to manage.

## Development

```powershell
dotnet restore src/PushADB/PushADB.csproj
dotnet build src/PushADB/PushADB.csproj -c Release
dotnet run --project src/PushADB/PushADB.csproj
```

Requires Windows and .NET 8 SDK for development.

## IPTV / Chromecast diagnosis

PushADB includes a **read-only ADB console** and seven diagnostic profiles. Connect an authorized device, select **Diagnostic complet (lecture seule)**, click **Analyser**, then **Exporter journal**. Profiles inspect reboot reasons, uptime, thermal service, memory, video codec, Wi-Fi/connectivity, application exit history and a recent logcat snapshot. Console commands are restricted to a supported diagnostic allowlist; this is not an unrestricted shell. A power dropout may leave no useful Android logs, so compare with a known-good original mains power adapter and cable. For freezes, test another player or temporarily disable hardware decoding in the IPTV application when available. Logs may contain IPs and package names; review before sharing.
