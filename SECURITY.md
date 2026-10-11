# Security Policy / Politique de sécurité

## Supported versions / Versions prises en charge

PushADB is in **preview**. There is no audited production release or guaranteed security support window.

PushADB est en **préversion**. Aucune édition n'a encore été auditée pour un usage en production.

## Reporting security issues / Signaler une faille

Please do **not** post credentials, ADB authorization keys, private device logs, pairing codes, or network topology in public issues. Use GitHub's **Report a vulnerability** private disclosure feature when enabled, or contact the repository maintainer privately.

Ne publiez jamais de secrets, codes d'appairage, clés ADB, journaux privés ni données réseau sensibles dans une issue publique.

## Security design

- All ADB operations must target an explicitly selected device.
- Administrative actions require user confirmation.
- APK downloads should be obtained from official publisher sources; a valid ZIP/APK header is **not** proof of authenticity.
- Debugging ports should only be enabled on trusted networks.
- No authorization bypass, DRM circumvention, or rooted system modification is intended.

## Known limitations

Device firmware can vary. Remote settings writes may be ignored or denied. Diagnostic logs can contain personally identifying or confidential information. Verify APK signatures and checksums independently when available. Windows binaries are not presently code-signed.
