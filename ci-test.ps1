$ErrorActionPreference = 'Stop'
$folder = Join-Path ([IO.Path]::GetTempPath()) ('MacroTracker-CI-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder | Out-Null
foreach ($mode in @('self-test','portion-test','update-test')) {
    $path = Join-Path $folder ($mode + '.db')
    $process = Start-Process -FilePath (Join-Path $PSScriptRoot 'MacroTracker.exe') -ArgumentList @('--' + $mode, '--data', ('"' + $path + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw ($mode + ' failed; see ' + $folder) }
}
$setupPath = Join-Path $folder 'setup-tests'
$setupProcess = Start-Process -FilePath (Join-Path $PSScriptRoot 'dist\MacroTracker-Setup.exe') -ArgumentList @('--self-test', ('"' + $setupPath + '"')) -WindowStyle Hidden -PassThru -Wait
if ($setupProcess.ExitCode -ne 0) { throw ('Setup test failed; see ' + $setupPath) }
Get-Content -LiteralPath (Join-Path $setupPath 'results.txt')
Write-Host 'Core, quantity/time, migration, updater integrity/rollback, and native installer checks passed.'
