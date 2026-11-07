param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('setup', 'verify', 'build')]
    [string]$Command,
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe',
    [string]$Project = (Split-Path -Parent $PSScriptRoot),
    [int]$TimeoutSeconds = 900
)
$ErrorActionPreference = 'Stop'
$Project = [System.IO.Path]::GetFullPath($Project)
if (!(Test-Path -LiteralPath $Unity)) { throw 'Supply -Unity with the Unity 6000.3.6f1 installation path.' }
$open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($Project, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($open) { throw 'Close this project in Unity before using the batch command wrapper.' }
$methods = @{ setup = 'SetupScene'; verify = 'VerifyProject'; build = 'BuildWindows' }
$artifacts = Join-Path $Project 'Artifacts'
[System.IO.Directory]::CreateDirectory($artifacts) | Out-Null
$log = Join-Path $artifacts ('batch-' + $Command + '.log')
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $Project + '"'),
    '-executeMethod', ('Rimbara.Editor.ProjectBuilder.' + $methods[$Command]), '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw ('Unity ' + $Command + ' timed out. See ' + $log)
}
if ($process.ExitCode -ne 0) { throw ('Unity ' + $Command + ' failed with exit code ' + $process.ExitCode + '. See ' + $log) }
Write-Output ('Unity ' + $Command + ': OK; ' + $log)
