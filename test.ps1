param([switch]$LiveUsda)
$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('MacroTracker-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$appPath = Join-Path $PSScriptRoot 'MacroTracker.exe'
$dataPath = Join-Path $testDirectory 'integration.db'
$integration = Start-Process -FilePath $appPath -ArgumentList @('--self-test', '--data', ('"' + $dataPath + '"')) -WindowStyle Hidden -PassThru -Wait
if ($integration.ExitCode -ne 0) { throw ('Integration checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($dataPath + '.results.txt')
$reopen = Start-Process -FilePath $appPath -ArgumentList @('--verify-persistence', '--data', ('"' + $dataPath + '"')) -WindowStyle Hidden -PassThru -Wait
if ($reopen.ExitCode -ne 0) { throw ('Process restart persistence checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($dataPath + '.reopen-results.txt')
$smokePath = Join-Path $testDirectory 'smoke.db'
$smoke = Start-Process -FilePath $appPath -ArgumentList @('--smoke', '--data', ('"' + $smokePath + '"')) -PassThru -Wait
if (($smoke.ExitCode -ne 0) -or -not (Test-Path -LiteralPath ($smokePath + '.smoke-results.txt'))) { throw ('UI checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($smokePath + '.smoke-results.txt')
$usdaPath = Join-Path $testDirectory 'usda.db'
$usdaArguments = @('--usda-test', '--data', ('"' + $usdaPath + '"'))
if ($LiveUsda) { $usdaArguments += '--live' }
$usda = Start-Process -FilePath $appPath -ArgumentList $usdaArguments -WindowStyle Hidden -PassThru -Wait
if ($usda.ExitCode -ne 0) { throw ('USDA checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($usdaPath + '.usda-results.txt')
$usdaUiPath = Join-Path $testDirectory 'usda-ui.db'
$usdaUi = Start-Process -FilePath $appPath -ArgumentList @('--usda-ui', '--data', ('"' + $usdaUiPath + '"')) -PassThru -Wait
if (($usdaUi.ExitCode -ne 0) -or -not (Test-Path -LiteralPath ($usdaUiPath + '.usda-ui-results.txt'))) { throw ('USDA UI checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($usdaUiPath + '.usda-ui-results.txt')
$portionPath = Join-Path $testDirectory 'portion.db'
$portion = Start-Process -FilePath $appPath -ArgumentList @('--portion-test', '--data', ('"' + $portionPath + '"')) -WindowStyle Hidden -PassThru -Wait
if ($portion.ExitCode -ne 0) { throw ('Quantity/time checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($portionPath + '.portion-results.txt')
$portionUiPath = Join-Path $testDirectory 'portion-ui.db'
$portionUi = Start-Process -FilePath $appPath -ArgumentList @('--portion-ui', '--data', ('"' + $portionUiPath + '"')) -PassThru -Wait
if (($portionUi.ExitCode -ne 0) -or -not (Test-Path -LiteralPath ($portionUiPath + '.portion-ui-results.txt'))) { throw ('Quantity/time UI checks failed. See ' + $testDirectory) }
Get-Content -LiteralPath ($portionUiPath + '.portion-ui-results.txt')
Write-Host ('Test data and screenshots: ' + $testDirectory)
