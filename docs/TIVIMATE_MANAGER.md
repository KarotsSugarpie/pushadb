# PushADB — TiviMate Manager (Windows / .NET 8)

## Scope

This is an **assisted** configuration and backup module, not an official TiviMate API integration.

- TiviMate package: ar.tvplayer.tv
- Target: an authorized network-ADB device using Android-based Fire OS or Android TV
- No root, no private app database access, no APK modification or license bypass
- No brittle screen coordinates or automatic guessing of menu positions
- Windows PushADB only for this initial version (the separate Linux preview remains unchanged)

## Configuration Xtream / M3U / EPG

1. Connecte ton Fire TV ou Android TV à PushADB et autorise ADB sur la TV.
2. Ouvre **Gestion TiviMate** puis **Détecter TiviMate** et **Ouvrir TiviMate**.
3. Navigue jusqu'à **Ajouter playlist > Xtream Codes** ou **Playlist M3U**, en utilisant l'onglet Télécommande ou ta télécommande réelle.
4. Place le curseur DANS le champ cible sur la TV.
5. Dans l'onglet Xtream/M3U/EPG, entre la valeur sur le PC, sélectionne le champ correspondant et clique **Envoyer le champ**.
6. Répète pour serveur, identifiant, mot de passe, playlist et EPG. Termine dans l'interface TiviMate.

Ce workflow est guidé : PushADB n'invente pas les menus ni les positions, qui changent selon les versions TiviMate. Il ne crée pas automatiquement la playlist sans intervention de l'utilisateur.

## Sauvegarde PC vers TV (.tmb)

1. Fais une sauvegarde de la configuration actuelle via TiviMate avant toute restauration.
2. Dans PushADB : **Sauvegardes .tmb > Envoyer une sauvegarde .tmb vers la TV**.
3. Choisis un fichier .tmb local. PushADB le copie dans /sdcard/Download avec un nom unique.
4. Dans TiviMate : **Paramètres > Général > Restaurer**. Ouvre le fichier dans Downloads, puis confirme dans TiviMate après lecture des avertissements.

**Important : le transfert ADB ne déclenche jamais la restauration automatiquement.** La restauration peut remplacer les paramètres existants.

## Sauvegarde TV vers PC (.tmb)

1. Crée une sauvegarde via **Paramètres > Général > Sauvegarder** dans TiviMate, vers Download ou Documents.
2. Dans PushADB : **Lister les sauvegardes .tmb**, puis choisis un fichier.
3. Clique **Récupérer sur le PC** et choisis le dossier local.

La détection couvre les fichiers directement présents dans /sdcard/Download et /sdcard/Documents, pas les sous-dossiers, NAS ou périphériques USB.

## Security and technical notes

- All device commands are explicitly scoped to the selected, authorized ADB endpoint.
- The dialog never saves playlist values or passwords to disk; password input is masked and cleared at closure.
- Generic events are logged; **credentials and full ADB text commands are never logged** by this module.
- ADB text input may put credentials in *local process arguments*. Privileged processes on the PC might read them. Only use a trusted PC and trusted network; for maximum privacy, type secrets via the TV remote.
- Android input text is not a full UTF-8 keyboard: non-ASCII and literal percent-s combinations are blocked rather than silently corrupted.
- The file extension .tmb alone cannot authenticate a backup; use trusted sources. Backups may themselves contain IPTV credentials.
- Version-specific TiviMate menu names, premium entitlement, backup format and device restrictions require hardware testing.

## Validation checklist (before release)

- [ ] Windows .NET 8 CI builds and packages PushADB
- [ ] Existing ADB connection, APK install, catalog, TweakUI and diagnostics still function
- [ ] Detect/launch TiviMate on Fire TV and Android TV
- [ ] Remote keys work for Up, Down, Left, Right, OK, Back, TAB and Enter
- [ ] Guided Xtream fields and M3U URL (including ampersand) work
- [ ] Unsupported Unicode/text variants fail safely, without logging secrets
- [ ] TiviMate can see and restore a .tmb copied into Downloads
- [ ] A .tmb saved into Downloads by TiviMate can be copied back to Windows
- [ ] Restoration is confirmed by user and existing data is preserved until confirmation
- [ ] Cancel, disconnect, and denied permissions produce no destructive action
- [ ] Vega OS is documented as incompatible with this Android ADB backend

## References

- TiviMate: https://tivimate.com/
- Android ADB: https://developer.android.com/tools/adb
- Community example of backup restore intent: https://github.com/vansmak/tivisync
