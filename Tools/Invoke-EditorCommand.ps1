param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('setup', 'verify', 'build', 'quit', 'assets')]
    [string]$Command,
    [int]$TimeoutSeconds = 900
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'Artifacts'
[System.IO.Directory]::CreateDirectory($artifacts) | Out-Null
$inbox = Join-Path $artifacts 'editor-command.v2.json'
$ready = Join-Path $artifacts 'editor-bridge-ready.txt'
if ((Test-Path -LiteralPath $inbox) -or (Test-Path -LiteralPath ($inbox + '.executing'))) {
    throw 'A previous Editor command is still pending. Do not overwrite its inbox.'
}
$unity = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($root, [StringComparison]::OrdinalIgnoreCase) -ge 0 } |
    ForEach-Object { Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue } |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (!$unity) { throw 'Open this RIMBARA project in Unity before using the Editor bridge.' }
$latestSource = (Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -File |
    Where-Object { $_.Extension -in @('.cs', '.asmdef') } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class RimbaraEditorFocus {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
}
'@
$watcher = New-Object System.IO.FileSystemWatcher($artifacts)
$watcher.EnableRaisingEvents = $true
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
try {
    [RimbaraEditorFocus]::SetForegroundWindow($unity.MainWindowHandle) | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('^r')
    while ($true) {
        if (Test-Path -LiteralPath $ready) {
            $readyFile = Get-Item -LiteralPath $ready
            if ($readyFile.LastWriteTimeUtc -ge $latestSource -and (Get-Content -LiteralPath $ready -Raw).StartsWith('3 /')) { break }
        }
        $remaining = [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds
        if ($remaining -le 0 -or $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, $remaining).TimedOut) {
            throw 'The latest Editor scripts did not compile before the deadline. Inspect the Unity Editor log.'
        }
    }
    $id = [Guid]::NewGuid().ToString('N')
    $result = Join-Path $artifacts ('editor-response-' + $id + '.json')
    $request = @{ Command = $Command; RequestId = $id } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inbox + '.new', $request)
    [System.IO.File]::Move($inbox + '.new', $inbox)
    Write-Output ('Queued RIMBARA Editor command: ' + $Command)
    while ($true) {
        if (Test-Path -LiteralPath $result) {
            $stream = [System.IO.File]::Open($result, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
            $reader = New-Object System.IO.StreamReader($stream)
            try { $response = $reader.ReadToEnd() | ConvertFrom-Json }
            finally { $reader.Dispose() }
            if ($response.RequestId -eq $id) {
                if (!$response.Success) { throw $response.Message }
                Write-Output $response.Message
                break
            }
        }
        $remaining = [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds
        if ($remaining -le 0 -or $watcher.WaitForChanged([System.IO.WatcherChangeTypes]::All, $remaining).TimedOut) {
            throw ('Unity did not complete ' + $Command + ' before the deadline. Inspect Artifacts and the Editor log.')
        }
    }
    if ($Command -eq 'quit') {
        $remaining = [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds
        if ($remaining -le 0 -or !$unity.WaitForExit($remaining)) {
            throw 'The RIMBARA Editor acknowledged quit but did not exit before the deadline.'
        }
    }
} finally { $watcher.Dispose() }
