Macro Tracker 1.3.1 improves installer reliability and error reporting.

- Creates shortcuts with the native Windows Shell API, removing reflected Windows scripting calls.
- Optional shortcut or existing startup-entry failures show specific warnings instead of aborting an otherwise completed file installation. The app is not automatically started when these warnings occur.
- Reports the underlying error and failing step, with a local diagnostic log when writable.
- Setup can be run again over a partial installation. Food history, settings, favorites, weight entries, and the encrypted USDA key are preserved.
- Adds automated embedded-package installation, repeat-install, native shortcut, and optional-failure checks alongside existing app and update tests.

If setup previously failed, exit any running Macro Tracker using its tray menu, then use **MacroTracker-Setup.exe** from this release. Already-installed users can use **Download update** in the app; setup is only needed to repair missing shortcuts or an incomplete first installation.

Windows 10/11 x64 with .NET Framework 4.8. This build is unsigned. The previous antivirus alert was not diagnosed; this release addresses installer code and does not claim to resolve that alert.

The ZIP is the portable/app-update package. SHA256SUMS.txt provides release checksums. The in-app updater uses GitHub's SHA-256 asset digest automatically.
