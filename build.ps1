$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Install/enable .NET Framework 4.8 on Windows 10 or 11.' }
$references = @('System.dll','System.Core.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.Security.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$arguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/win32icon:app.ico','/out:MacroTracker.exe','/resource:LabelOcr.ps1,LabelOcr')
$arguments += $references | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$arguments += @('App.cs','LibraryStore.cs','LibraryViews.cs','IngredientEditor.cs','ModernDashboard.cs','LabelScan.cs','CameraCapture.cs','FeatureTests.cs','FeatureUiTests.cs','Store.cs','MainWindow.cs','ReminderSettings.cs','SoftDatePicker.cs','SoftStyles.cs','Usda.cs','UsdaWindow.cs','UsdaTests.cs','FoodDialog.cs','FoodPortion.cs','PortionTests.cs','ReleaseInfo.cs','Updates.cs','UpdatePackage.cs','UpdateTests.cs')
Push-Location $PSScriptRoot
try {
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $helperArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+','/win32icon:app.ico','/out:MacroTracker.Updater.exe')
    $helperArgs += @('System.dll','System.Core.dll','System.Windows.Forms.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
    $helperArgs += @('UpdaterProgram.cs','UpdatePackage.cs','ReleaseInfo.cs')
    & $compiler @helperArgs
    if ($LASTEXITCODE -ne 0) { throw 'Updater build failed.' }
    Write-Host 'Built MacroTracker.exe'
} finally { Pop-Location }
