param(
    [ValidateSet('full', 'week1')]
    [string]$Profile = 'full',
    [string]$Executable = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Builds\Windows\RIMBARA.exe'),
    [string]$Output,
    [int]$TimeoutSeconds = 180
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$Output) { $Output = Join-Path $root ('Artifacts\smoke-' + $Profile) }
$Output = [System.IO.Path]::GetFullPath($Output)
if (!(Test-Path -LiteralPath $Executable)) { throw 'Build the Windows executable before running standalone smoke tests.' }
[System.IO.Directory]::CreateDirectory($Output) | Out-Null
$log = Join-Path $Output 'player.log'
$reportPath = Join-Path $Output 'report.json'
$arguments = @('--rimbara-smoke', ('--rimbara-profile=' + $Profile), ('--rimbara-output="' + $Output + '"'),
    '-screen-width', '1600', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
$started = [DateTime]::UtcNow
$process = Start-Process -FilePath $Executable -ArgumentList $arguments -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw ('RIMBARA runtime test timed out. See ' + $log)
}
if (!(Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $started) {
    throw ('No new runtime report was written. Exit code: ' + $process.ExitCode + '. See ' + $log)
}
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($process.ExitCode -ne 0 -or !$report.Passed) {
    $failures = @($report.Checks | Where-Object { !$_.Passed } | ForEach-Object { $_.Name }) -join ', '
    throw ('Runtime checks failed: ' + $failures + '. See ' + $reportPath)
}
Write-Output ('RIMBARA ' + $Profile + ' runtime checks passed: ' + $report.Checks.Count + '; ' + $reportPath)
