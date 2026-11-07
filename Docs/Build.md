# Build and verification commands

## Prerequisites

- Unity **6000.3.6f1**, a valid Unity licence, and Windows Build Support (Mono).
- Git and Git LFS; download LFS objects before opening a clone.
- Use the RIMBARA project root, not the parent workspace or Forest Reborn.

## With the Editor open

Use the **RIMBARA** menu to verify or build. Main must be saved before building.
The existing authored scene is used; building does not regenerate it.

The local PowerShell bridge can use the already-open RIMBARA Editor:

```powershell
.\Tools\Invoke-EditorCommand.ps1 -Command verify
.\Tools\Invoke-EditorCommand.ps1 -Command build
```

It refreshes scripts, waits for a matching command response through filesystem
events, and writes local diagnostics under `Artifacts`. Open the correct
project first. Do not queue another command while an inbox is pending.

## With the Editor closed

Close the Editor before batch commands; Unity cannot open the same project
twice. The wrapper checks this and waits for the exact process to exit. Supply
`-Unity` if your Unity installation is in a different location:

```powershell
.\Tools\Invoke-UnityCommand.ps1 -Command verify
.\Tools\Invoke-UnityCommand.ps1 -Command build
```

For explicit first-time regeneration, use `-Command setup`; it has the same
overwrite warning as the Editor's regeneration menu. Normal clones already
include Main and all required resources and do not need regeneration.

Equivalent Unity CLI commands are:

```powershell
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe'
$Project = (Get-Location).Path
New-Item -ItemType Directory -Path '.\Artifacts' -Force | Out-Null

& $Unity -batchmode -quit -projectPath $Project `
  -executeMethod Rimbara.Editor.ProjectBuilder.VerifyProject `
  -logFile "$Project\Artifacts\verify.log"

& $Unity -batchmode -quit -projectPath $Project `
  -executeMethod Rimbara.Editor.ProjectBuilder.BuildWindows `
  -logFile "$Project\Artifacts\build.log"

& $Unity -batchmode -nographics -projectPath $Project `
  -runTests -testPlatform EditMode `
  -testResults "$Project\Artifacts\editmode-tests.xml" `
  -logFile "$Project\Artifacts\editmode-tests.log"
```

When using PowerShell interactively, wait for the Unity process to finish
before starting the next command. Unit-test execution is responsible for its
own exit; do not add `-quit` to the test-runner command.

## Standalone runtime smoke checks

Runtime diagnostics are opt-in and do not run in normal gameplay:

```powershell
.\Builds\Windows\RIMBARA.exe --rimbara-smoke `
  --rimbara-profile=full --rimbara-output="$Project\Artifacts\smoke-full"

.\Builds\Windows\RIMBARA.exe --rimbara-smoke `
  --rimbara-profile=week1 --rimbara-output="$Project\Artifacts\smoke-week1"
```

The PowerShell wrappers wait for completion and reject failed or stale reports:

```powershell
.\Tools\Test-EditMode.ps1
.\Tools\Test-Runtime.ps1 -Profile full
.\Tools\Test-Runtime.ps1 -Profile week1
```

Close this project's Editor before `Test-EditMode.ps1`. Standalone smoke runs
create virtual keyboard/mouse devices, exercise the real action bindings and
official motor, walk the mission route, and save PNG captures plus `report.json`.
Temporary obstruction fixtures and input overrides exist only in smoke mode.
Do not use its measured route time as a human-playtest session duration.

Reports/captures and exit status are the evidence to check, not merely the
presence of an executable. See `Docs/Verification.md` for the actual completed
verification record and remaining limitations.

## Output

The local Windows build is **`Builds/Windows/RIMBARA.exe`**. Keep the whole
Windows folder together when copying the game. Builds, caches, credentials,
logs, and raw diagnostics are intentionally not committed.

For a fresh-clone check, clone the committed repository to a different folder,
run `git lfs pull` and `git lfs fsck`, then run the batch project verification
and EditMode tests using that clone's project path. This catches missing
metadata/assets that an existing Library cache could conceal.
