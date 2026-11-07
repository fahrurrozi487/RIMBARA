# Verification record

The full MVP and Week 1 profile both completed their automated standalone
routes, returned to the volunteer post, reached victory, and replayed cleanly.
These results apply to the latest authored scene and Windows build, including
the UI pointer tests, corrected volume slider, and tree-avoiding Week 1 route.

## Completed checks

| Verification | Result | Preserved evidence |
| --- | --- | --- |
| Authored scene and deterministic model | **56/56 passed** | [Report](Evidence/project-verification.json) |
| Unity EditMode tests | **36/36 passed**, no failures or skipped cases | [NUnit XML](Evidence/editmode-tests.xml) |
| Windows x64 player build | **Succeeded**, 0 warnings, 0 errors | [Build report](Evidence/build-report.json) |
| Full MVP runtime (5 seedlings / 5 trees / 2 fires) | **79/79 passed**, no runtime error/exception/assert logs | [Full report](Evidence/full-runtime.json) |
| Week 1 runtime (1 seedling / 1 tree / 1 fire) | **69/69 passed**, no runtime error/exception/assert logs | [Week 1 report](Evidence/week1-runtime.json) |
| Unity asset metadata | **353 assets / 442 metadata files**; no missing metadata, invalid GUIDs, or duplicate GUIDs | [Asset audit](Evidence/asset-audit.json) |
| Official controller provenance | Both runtime scripts **byte-for-byte identical** to the official 2.0.2 package | [SHA-256 record](Evidence/official-controller-provenance.json) |
| Windows distribution support and licences | Executable, data, UnityPlayer, and all three licence notices present | [Distribution audit](Evidence/distribution-audit.json) |
| Local Git LFS integrity | **126 tracked files / 125 unique objects**; all checked out, zero SHA-256 mismatches, `git lfs fsck` passed | [LFS audit](Evidence/lfs-audit.json) |
| Local Windows ZIP | **176 files**, 41,488,713 bytes; archive CRC/integrity check passed | [Archive audit](Evidence/distribution-archive.json) |
| Independent HTTPS/Git LFS clone | **Passed**: fresh import, 56 scene/model checks, 36 unit tests, all LFS/vendor hashes intact | [Clone audit](Evidence/fresh-clone.json) |

The test environment used **Unity 6000.3.6f1**, Windows x64, an **AMD Radeon
RX 6600**, and **Direct3D 11** at **1600 × 900**, windowed. Recorded UTC times:
structural verification `2026-11-07T17:04:31Z`, EditMode `17:09:22Z`, full runtime
`17:09:16Z`, and Week 1 runtime `17:10:38Z` on the same date.

Primary package versions are URP 17.3.0, Cinemachine 3.1.7, Input System 1.18.0,
uGUI/TextMeshPro 2.0.0, and Test Framework 1.6.0. The manifest and resolved lock
are committed. The full-profile scene has three authored zones and the exact
5/5/2 object supply; Week 1 disables surplus objects.

## Runtime coverage and test boundaries

- Virtual keyboard and mouse events exercise WASD, normalized diagonal input,
  Shift running, animation blend parameters, mouse camera orbit/pitch clamping,
  E pickup/planting, left-mouse water, and Escape pause/resume.
- UI transitions use pointer movement and mouse-button events through Unity's
  EventSystem, not direct button callback invocation. The pause volume slider
  is tested with a pointer click as well as a value change.
- The mission route moves through the actual official motor and colliders.
  Physical inspection and post triggers enforce all four ordered missions.
  The route walks around planted trunks rather than disabling their collision.
- Nearby interaction, empty inventory, consumed inventory, distinct object IDs,
  capped counters, blocked interaction, and duplicate reward rejection pass.
- Water checks cover crosshair selection, range, camera-ray obstruction,
  independent nozzle obstruction, particles, retained contact after release
  and pause, and one-shot fire/visual/collider completion.
- Pause freezes gameplay, animation and mission time. Recovery preserves
  progress. Reaching 100% does not win until returning to the post. Victory
  releases controls/cursor; replay and menu reload restore objects and counters.

The driver is installed **only with `--rimbara-smoke`**. It creates temporary
virtual devices and obstruction fixtures, directly checks rejection APIs, and
uses teleporting for isolated boundary/gating fixtures. Its mission traversal
uses physical movement; it is not a human playtest or proof of every possible
player route. Neither fixtures nor automatic actions run during normal play.

Measured gameplay frame times were approximately **10.04 ms mean / 10.01 ms
P95** for full and **10.06 ms mean / 10.01 ms P95** for Week 1 on this machine.
These short, frame-capped runs are diagnostic observations, not a benchmark or
a performance guarantee for other hardware.

## Visual review

Final captures were reviewed for readable HUD panels, map dots, restoration
bars, the circular volume handle, character/tool pose, water/fire effects, and
correct profile-specific victory counts:

- [Main menu](Captures/menu.png)
- [Pause and volume](Captures/pause.png)
- [Nearby seedling interaction](Captures/seedling.png)
- [Planted tree and new ground cover](Captures/planting.png)
- [Water suppression and 5/5 counters](Captures/water.png)
- [Full 5-tree / 2-fire victory](Captures/victory.png)
- [Week 1 1-tree / 1-fire victory](Captures/week1-victory.png)

Each runtime report records eight captures; representative final images are
committed above. Superseded failed diagnostic runs are not used as evidence.

## Source and reproduction

The official Starter Assets package is product **196526**, version **2.0.2**,
**80,315,533 bytes**. Git exempts vendor scripts from line-ending conversion so
the two recorded hashes remain reproducible after checkout. Kenney CC0,
LiberationSans OFL, and Unity Companion notices are preserved in source and
copied into the local Windows distribution. Mixamo remains licensed, not CC0;
the repository is private and has no invited Persons B/C.

Use the [build/test commands](Build.md) to reproduce the checks. Normal clones
already include Main, prefabs, fonts, textures, audio and profiles; **do not
regenerate Main merely to import or play the project**.

## Executed fresh-clone verification

The published source commit
**`12ac408289d92fdc6e177f7de17aae2721b3cc9b`** was cloned over HTTPS to a new
temporary directory, initially skipping smudge so that `git lfs pull` could
explicitly download the binary assets from GitHub. No Git object alternates,
shared LFS storage, or pre-existing Unity Library were used. Unity's normal
machine-level package cache is allowed; the project's imported Library was
created from scratch.

- **126 LFS files / 125 unique objects** downloaded and checked out;
  `git lfs fsck` and all checkout SHA-256 comparisons passed.
- **353 assets / 442 metadata files** were present. The two official controller
  hashes, Main's hash, manifest, and resolved package lock matched the verified
  source. Main was **not regenerated**.
- First-import verification passed **56/56** checks at
  `2026-11-07T17:20:04Z` ([scene report](Evidence/fresh-clone-project-verification.json)).
- EditMode passed **36/36**, with zero failures/skipped cases, at
  `2026-11-07T17:20:42Z` ([unit XML](Evidence/fresh-clone-editmode-tests.xml)).
- No tracked content changes remained after import/tests; the clone's Git
  working tree was clean. Final follow-up changes only add documentation and
  evidence, not gameplay code, scenes, packages, or project settings.

The [combined clone audit](Evidence/fresh-clone.json) records the actual checks.
To repeat them in an independent folder:

```powershell
git clone https://github.com/fahrurrozi487/RIMBARA.git RIMBARA-verify
cd RIMBARA-verify
git lfs pull
git lfs fsck
.\Tools\Invoke-UnityCommand.ps1 -Command verify
.\Tools\Test-EditMode.ps1
```

## Known design limitations

- Small authored map, not an open world; boundary rails and fall recovery are intentional.
- Fire and tree growth are stylized gameplay effects, not realistic simulations.
- Wind, birds, fire, water, and feedback are synthesized original audio.
- No persistent mission save; replay, return-to-menu, and quit reset the session.
- Keyboard/mouse is the supported control scheme.
- The presentation's 10–15-minute session target is not an established human-playtest result.
- Windows x64 is the delivery target; other platforms have not been verified.

The automated victory captures show **01:28** field time for full and **00:53**
for Week 1. The driver deliberately sprints and follows known destinations;
those times must not be presented as a 10–15-minute human session validation.
