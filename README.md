# Macro Tracker for Windows

A local desktop calorie and macro tracker with USDA food search, editable quantities and meal times, reminders, history, weight tracking, and in-app updates.

## Install once

Download **MacroTracker-Setup.exe** from [the latest release](https://github.com/h-covert/macro-tracker/releases/latest). Exit any older copy using its tray menu → **Exit**, then run setup once. It installs in `%LOCALAPPDATA%\Programs\MacroTracker` and adds Desktop and Start-menu shortcuts. No administrator access is needed.

Requires Windows 10/11 x64 and .NET Framework 4.8. SQLite is supplied by Windows. This build is unsigned.

Version 1.3.1 replaces reflected Windows scripting calls with the native Windows Shell shortcut API. Optional Desktop/Start-menu shortcut or existing startup-entry failures show specific warnings without treating copied application files as a failed installation. When optional steps fail, setup reports the installed location and does not start the app automatically. Running setup again repairs program files and retries shortcuts; diary data and the USDA key are preserved.

Setup reports the failing step and underlying exception, with local `MacroTracker-setup-error-*.log` or `MacroTracker-setup-warning-*.log` files in the Windows temporary folder when writable. Logs can contain Windows account names and local paths; review them before sharing. An antivirus alert reported during a v1.3.0 installation remains undiagnosed; these installer changes do not certify antivirus compatibility or change security settings.

## Future updates: no reinstall

When Macro Tracker opens, it checks this repository's latest stable release. If you enabled **Launch Macro Tracker when Windows starts**, this also happens at Windows sign-in. The app itself has no login or cloud account.

When a newer version is available, click **Download update**. The app downloads the update ZIP, checks GitHub's SHA-256 digest and the executable version, backs up the diary, closes, replaces the program files, and restarts. If replacement fails, it attempts to restore the previous program files and reports the error. Your diary and encrypted USDA key stay in their separate data folder.

Settings → **App updates** offers a manual check and a switch for automatic checks. Offline checks fail quietly. Updates install only when you click the button. Finish any open food entry first. Unsafe ZIP paths, unexpected filenames, oversized packages, bad hashes, and version mismatches are rejected.

The first setup adds this capability to older portable versions. After that, use the installed shortcut and in-app updates. No need to run setup for each release.

## Daily use

- **Dashboard:** calories, protein, carbs, and fat consumed/target/remaining.
- **Add food / Ctrl+N:** enter a food or select **Search USDA foods**. Missing nutrients stay blank for review. Save favorites for offline reuse.
- **Quantity:** macros are entered for one item/serving; quantity scales the total. For two eggs, define `1 whole egg` with per-egg macros and quantity `2`. USDA household portions such as `1 large` are offered where available.
- **Time eaten:** editable for new and existing entries. Accepts `8:15 AM`, `7:10 PM`, or `19:10`. The log sorts by meal time. Use the date picker for another day.
- **History:** daily results and Monday–Sunday averages using days with food entries. Empty days don't count as successes; today's partial log is included.
- **Weight:** optional lb/kg entries, latest/previous, a seven-calendar-day mean using recorded weigh-ins, and a 30-entry trend. Weight storage/CSV use kg.

Calories are independent of macros, including for drinks. Defaults are 2,450 kcal / 190g protein / 240g carbs / 75g fat. Existing days retain their target snapshots when defaults change. A new day starts empty without deleting history. Adding food on a previously unrecorded past date uses the current defaults.

See [quantity/time details](QUANTITY-AND-TIME.md) and [USDA setup](USDA-SETUP.md). Older entries migrate as quantity 1 representing the entire originally logged portion; the app doesn't infer counts from free text.

## USDA search

The official demo key works immediately. For regular use, get a free key from [api.data.gov/signup](https://api.data.gov/signup/) and paste it directly into **Settings → Set up USDA search**. It is encrypted for your Windows account in a separate local file. No personal key is shipped in this repository.

New searches/portion lookups require internet and send your query to USDA. Your diary, weight, and targets are not sent. Saved foods work offline. Check the description, brand, and raw/cooked state. Portion weights come from USDA; the app never guesses mass/volume conversions.

[USDA's API guide](https://fdc.nal.usda.gov/api-guide/) documents current limits: demo 30 requests/hour and 50/day per IP; personal keys default to 1,000/hour per IP.

## Reminders and startup

Defaults: 08:00, 12:30, 15:30, 19:00, and 21:00. Configure individual times, messages, switches, and snoozes in Settings. Tray options include Open, Quick Add, Today's Progress, Snooze, and Exit. Closing hides the app in the tray by default; Exit stops reminders.

Windows tray notifications can be suppressed by Do Not Disturb or notification settings. Use **Test notification** on your computer. Clicking a notification opens Quick Add when Windows delivers the click event. The app must be running and Windows awake: ordinary missed reminders aren't replayed, while due snoozes are. The scheduler checks every 15 seconds, once per reminder per local date, with simple protein/gap context.

Windows startup is optional and disabled by default. Setup redirects an existing Macro Tracker startup entry to the installed location without enabling startup for users who hadn't opted in.

## Data and recovery

Diary: `%LOCALAPPDATA%\MacroTracker\macro-tracker.db`. USDA key: the same path plus `.usda-key`. Updates never replace these files.

Settings offers full SQLite backup/restore, food CSV, and weight CSV. Restore validates the backup and saves a `.before-restore.bak` copy first. Migration/update safety copies use `.before-quantity-upgrade.bak` and `.before-update.bak`. These are single safety copies; keep dated backups for long-term recovery. Keys are excluded from database backups and must be reentered on another Windows account/PC.

Food CSV includes total macros, quantity, and time eaten. CSV exports are reports, not imports. Advanced isolated usage: `MacroTracker.exe --data "C:\path\separate.db"`; updates preserve this path. Startup errors are logged beside the selected database.

## Build and test

No SDK or NuGet download is needed. The build uses the compiler included with .NET Framework.

```powershell
powershell -ExecutionPolicy Bypass -File .\package.ps1
powershell -ExecutionPolicy Bypass -File .\ci-test.ps1
```

`package.ps1` builds the app, independent updater, portable/update ZIP, setup executable, and SHA256SUMS.txt in `dist/`. `ci-test.ps1` checks core behavior, quantities/times, migration, and updater integrity/rollback. `test.ps1` also opens test windows and tests key storage; add `-LiveUsda` for live demo-key requests. All tests use isolated data.

## Publish the next update

1. Make and test the change.
2. Increase all three version values in `ReleaseInfo.cs` and update `RELEASE-NOTES.md`.
3. Commit/push, then push the matching tag, for example `v1.3.1`.
4. GitHub Actions builds on Windows, runs checks, and publishes setup and ZIP assets. The workflow can also be run manually.

The workflow rejects mismatched tags and doesn't overwrite an existing release. Keep the ZIP named **MacroTracker-Windows-x64.zip** with the exact three files produced by `package.ps1`. Users are offered only newer stable releases with this asset and GitHub's digest. Editing source alone doesn't create an update.

## Source layout

| Files | Purpose |
| --- | --- |
| Store.cs | SQLite, migration, reminders, exports, backups |
| MainWindow.cs, FoodDialog.cs, FoodPortion.cs | Screens, quantities, times, tray |
| Usda.cs, UsdaWindow.cs | USDA client, portions, encrypted key storage |
| Updates.cs, UpdatePackage.cs, UpdaterProgram.cs | Release checks, download, validation, replacement/rollback |
| SetupProgram.cs, ReleaseInfo.cs | Per-user setup and version/repository constants |
| App.cs, *Tests.cs | Startup and isolated integration/UI tests |

The update source is fixed to this repository. Protect its write access: published executable releases are trusted software for installed users. Release hashes check integrity; they are not an independent code-signing identity. Windows notification delivery and login startup depend on the destination computer's settings.
