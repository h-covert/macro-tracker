$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & .\build.ps1
    New-Item -ItemType Directory -Path 'dist' -Force | Out-Null
    Compress-Archive -LiteralPath 'MacroTracker.exe','MacroTracker.exe.config','MacroTracker.Updater.exe' -DestinationPath 'dist\MacroTracker-Windows-x64.zip' -Force
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    $setupArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+','/win32icon:app.ico','/out:dist\MacroTracker-Setup.exe','/resource:dist\MacroTracker-Windows-x64.zip,AppPackage')
    $setupArgs += @('System.dll','System.Core.dll','System.Windows.Forms.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
    $setupArgs += @('SetupProgram.cs','ShellShortcut.cs','SetupTests.cs','UpdatePackage.cs','ReleaseInfo.cs')
    & (Join-Path $framework 'csc.exe') @setupArgs
    if ($LASTEXITCODE -ne 0) { throw 'Setup build failed.' }
    Get-FileHash 'dist\MacroTracker-Windows-x64.zip','dist\MacroTracker-Setup.exe' -Algorithm SHA256 |
        ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_.Path) } |
        Set-Content -LiteralPath 'dist\SHA256SUMS.txt' -Encoding ascii
    Write-Host 'Created setup, update package, and checksums in dist.'
} finally { Pop-Location }
