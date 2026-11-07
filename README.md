# RIMBARA
## Rise of the Forgotten Forest

A small third-person restoration game set in Arunika Forest. A young volunteer
inspects the aftermath of a fire, gathers native seedlings, plants new trees,
extinguishes the remaining flames, and returns to the volunteer post.

This is the separate **solo RIMBARA project**. It does not modify the earlier
**Forest Reborn** prototype.

![RIMBARA main menu in the verified Windows build](Docs/Captures/menu.png)

## Open and play

1. Install **Unity 6000.3.6f1 (Unity 6.3 LTS)** through Unity Hub, including
   Windows Build Support (Mono).
2. Clone the private repository with an account that has access:

   ```powershell
   git lfs install
   git clone https://github.com/fahrurrozi487/RIMBARA.git
   cd RIMBARA
   git lfs pull
   ```

3. Add the `RIMBARA` folder to Unity Hub. It contains `Assets`, `Packages`, and
   `ProjectSettings`; do not select the sibling Forest Reborn folder.
4. Open **`Assets/Scenes/Main.unity`** and press **Play**.
5. Select **Begin Restoration**, read the briefing, then **Enter Arunika**.

After building, launch **`Builds/Windows/RIMBARA.exe`** or `Play.cmd`.
Keep the executable beside its `_Data` folder and Unity support files.

## Controls

| Input | Action |
| --- | --- |
| WASD | Move relative to the camera |
| Mouse | Orbit the third-person camera |
| Shift | Run |
| E | Collect, plant, or read a nearby mission board |
| Left mouse, held | Spray water at the crosshair during Stop the Fire |
| Esc | Pause/resume; release the mouse cursor |

Move within **2.5 m** to interact and within **7 m** to extinguish a fire. World
objects can obstruct interactions and the water stream. Follow the amber
destination guide, green interaction markers, and the field map.

## Full MVP

- Three authored zones: damaged forest/post, planting clearings, remaining fires.
- **5 seedlings collected, 5 trees planted, 2 fires extinguished.**
- Four ordered missions:
  1. **First Steps** — reach the inspection marker.
  2. **Seeds of Hope** — collect and plant.
  3. **Stop the Fire** — aim and hold left mouse on each fire.
  4. **A New Beginning** — return to the volunteer post to win.
- Real Unity **Starter Assets ThirdPersonController**, Cinemachine 3, and a
  **Mixamo Y Bot** Humanoid with idle/walk/run animations.
- TextMeshPro mission HUD, inventory/counters, restoration bar, feedback,
  field map, main menu, briefing, controls, pause, volume, and ending screen.
- Growing planted trees/ground cover, Unity fire/water/steam particles, and
  original procedural wind, bird, fire, water, and feedback sounds.

Restoration is **(trees planted + fires extinguished) / 7** for the full MVP.
Seedlings supply planting inventory; they are not extra restoration points.
Reaching 100% alone does not win: you must return to the post.

There is **no damage, ammunition limit, time limit, progress decay, or fail
condition**. Falling outside the map recovers the volunteer without deleting
mission progress. Pause preserves the current session; replay/menu reload the
scene. Only the master volume is saved between launches.

## Week 1 profile

`Assets/Settings/WeekOne.asset` provides the **1 seedling / 1 tree / 1 fire**
vertical slice while retaining the same four mission stages.

```powershell
.\Builds\Windows\RIMBARA.exe --rimbara-profile=week1
```

For Inspector-based testing, assign `WeekOne` to the `GameState` profile on
**Game systems - profiles, missions and flow**. Restore `ArunikaMVP` for the
full game. A command-line `--rimbara-profile=full` or `week1` overrides the
Inspector; without an override, the assigned profile is preserved.

## Build, verify, and edit

- **RIMBARA > Verify authored project** checks the scene, dependencies,
  references, map supply, and deterministic mission/progress logic.
- **RIMBARA > Build Windows x64** builds the existing editable Main scene.
- Unity's **Test Runner > EditMode > Run All** runs the automated unit tests.
- **RIMBARA > Regenerate authored Main scene** is a development generator,
  not a requirement for playing. **It replaces Main and the generated
  volunteer/interaction prefabs. Save copies of your edits before using it.**

See [build instructions](Docs/Build.md), [project architecture](Docs/Architecture.md),
[presentation scope mapping](Docs/Scope.md), [verification and limitations](Docs/Verification.md), and
[third-party notices](THIRD_PARTY_NOTICES.md).

Pinned primary packages are URP **17.3.0**, Cinemachine **3.1.7**, Input System
**1.18.0**, uGUI/TextMeshPro **2.0.0**, and Test Framework **1.6.0**. The official
Starter Assets import resolved the Input System dependency to 1.18.0.
Unity caches, builds, logs, and local diagnostics are ignored by Git; binary
source assets are tracked through Git LFS. Selected final reports and captures
are preserved under `Docs/Evidence` and `Docs/Captures`.

Verified on the latest Windows build: **56 scene/model checks**, **36 EditMode
tests**, **79 full-MVP runtime checks**, and **69 Week 1 runtime checks**, all
passed. Both profiles reach return-to-post victory and replay cleanly.

## Scope and licensing

This is a focused solo MVP, not an open world or a realistic fire simulation.
It intentionally excludes weather/seasons/day-night, animal AI, multiplayer,
cloud saves/leaderboards, complex inventory, crafting, and skill trees.

Kenney environment assets are CC0. Mixamo and Unity Starter Assets retain
their own licences; **this is not an entirely open-source asset pack**. Keep
the private repository private unless you first review redistribution rights
or replace licensed assets. No Persons B/C or other collaborators are invited.
