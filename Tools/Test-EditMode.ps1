param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe',
    [string]$Project = (Split-Path -Parent $PSScriptRoot),
    [int]$TimeoutSeconds = 900
)
$ErrorActionPreference = 'Stop'
$Project = [System.IO.Path]::GetFullPath($Project)
if (!(Test-Path -LiteralPath $Unity)) { throw 'Unity 6000.3.6f1 was not found. Supply -Unity with the correct installation path.' }
$open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.Contains($Project) }
if ($open) { throw 'Close the Unity Editor for this project before running batch tests.' }
$artifacts = Join-Path $Project 'Artifacts'
[System.IO.Directory]::CreateDirectory($artifacts) | Out-Null
$results = Join-Path $artifacts 'editmode-tests.xml'
$log = Join-Path $artifacts 'editmode-tests.log'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $Project + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-testResults', ('"' + $results + '"'), '-logFile', ('"' + $log + '"'))
$started = [DateTime]::UtcNow
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw ('Unity unit tests timed out. See ' + $log)
}
if (!(Test-Path -LiteralPath $results) -or (Get-Item -LiteralPath $results).LastWriteTimeUtc -lt $started) {
    throw ('Unity did not produce a new test report. Exit code: ' + $process.ExitCode + '. See ' + $log)
}
[xml]$report = Get-Content -LiteralPath $results -Raw
$run = $report.'test-run'
if ($process.ExitCode -ne 0 -or [int]$run.failed -ne 0 -or [int]$run.passed -eq 0) {
    throw ('EditMode tests failed: ' + $run.failed + ' failures. See ' + $results)
}
Write-Output ('EditMode tests passed: ' + $run.passed + ' / ' + $run.total + '; ' + $results)
