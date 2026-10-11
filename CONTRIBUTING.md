# Contributing / Contribuer à PushADB

Thank you for helping improve PushADB. This project is an **early preview**; improvements to stability, compatibility, documentation and test coverage are particularly welcome.

Merci de contribuer à PushADB. Ce projet est une **préversion**. Les correctifs de stabilité, de compatibilité et les tests sont prioritaires.

## Bug reports / Rapports de bogues

Include:
- Operating system (Windows 10/11, Ubuntu, Debian, etc.) and app commit/version
- Target device model, OS version, ADB port and state (device, offline, unauthorized)
- Expected behavior versus observed behavior
- Safe, repeatable steps
- Sanitized excerpts of logs with private details removed

Précisez le système, le modèle, la version Android/Fire OS, le statut ADB, les étapes de reproduction et des journaux anonymisés.

## Development

Windows: `dotnet restore src/PushADB/PushADB.csproj`, then `dotnet build src/PushADB/PushADB.csproj`.

Linux: `python3 -m py_compile linux/pushadb.py` and `bash build-linux.sh`.

Never assume Windows Forms is cross-platform: the Ubuntu edition has a separate Tkinter implementation. New features should be explicitly scoped to one or both front ends.

## Pull requests

Keep changes small; document impact, platform limitations, expected permissions and rollback behavior. Any new command that changes device settings must require confirmation. No credential or ADB trust bypass code will be accepted.
