# Recorded verification evidence

These are snapshots of completed checks, not files that run the game.
`../Verification.md` explains the results and their limitations. The executable
and raw logs remain local under the ignored `Builds` and `Artifacts` folders.

- `project-verification.json`: all 56 authored-scene and model checks.
- `editmode-tests.xml`: all 36 EditMode test cases and their results.
- `full-runtime.json`: 79 standalone checks for the 5/5/2 profile.
- `week1-runtime.json`: 69 standalone checks for the 1/1/1 profile.
- `build-report.json`: successful Windows build, zero warnings/errors.
- `distribution-audit.json`: build support/licence checks and SHA-256 hashes.
- `distribution-archive.json`: the local 176-file Windows ZIP and its successful
  archive-integrity check; the archive itself is not committed.
- `asset-audit.json`: 353 Unity asset files, 442 metadata files, no missing
  metadata, invalid GUIDs, or duplicated GUIDs.
- `official-controller-provenance.json`: two vendor scripts matched against
  the official Unity package, product 196526, version 2.0.2.
- `lfs-audit.json`: all 126 binary-source/capture files checked out, 125 unique
  objects, zero SHA-256 mismatches, and a successful local `git lfs fsck`.
- `fresh-clone.json`: executed HTTPS/LFS clone audit, independent caches,
  unchanged Main/vendor/package files, and successful import/scene/unit checks.
- `fresh-clone-project-verification.json`: all 56 checks after the first import.
- `fresh-clone-editmode-tests.xml`: all 36 tests passed in the separate clone.

Machine-specific paths in the build and unit-test reports have been made
project-relative. Report timestamps are the actual UTC times returned by the
test environment. Runtime report capture names refer to each original
`Artifacts/smoke-full` or `Artifacts/smoke-week1` output folder; representative
images are preserved under `../Captures`.

Regenerate current diagnostics with the commands in `../Build.md`. Historical
snapshots do not replace running those commands after changing the project.
