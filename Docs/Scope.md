# Presentation and MVP scope

## Narrative and mission mapping

The volunteer arrives at the Arunika post after a forest fire. Restoration is
care, not combat: there is no damage, fail state, water ammunition, or mission
timer. Progress comes from planting native trees and extinguishing the last
flames, followed by returning to the post.

| Presentation requirement | Editable implementation |
| --- | --- |
| Third-person Unity 3D LTS game | Unity 6000.3.6f1, URP 17.3.0, `Assets/Scenes/Main.unity` |
| Three zones | Zone A: post/damage; Zone B: nursery/planting; Zone C: fires |
| First Steps | Physical inspection-marker trigger |
| Seeds of Hope | Nearby E pickup and planting, with consumed seedling inventory |
| Stop the Fire | Crosshair aim, held left mouse, range and obstruction checks |
| A New Beginning | Physical return-to-post trigger after the restoration goals |
| Full MVP targets | `Assets/Settings/ArunikaMVP.asset`: 5 seedlings, 5 trees, 2 fires |
| Week 1 vertical slice | `Assets/Settings/WeekOne.asset`: 1 seedling, 1 tree, 1 fire |
| Restoration formula | (trees planted + fires extinguished) / (tree goal + fire goal); full denominator 7 |
| Movement/camera controls | WASD, mouse orbit, Shift run, Esc pause |
| Official controller | Unmodified `StarterAssets.ThirdPersonController` with a typed adapter |
| Character/animation | Mixamo Y Bot Humanoid, idle/walk/run clips |
| Free low-poly environment | Kenney Nature Kit 2.1 CC0 meshes |
| Particle effects | Unity flame, smoke, water and steam systems |
| Mission HUD | Event-driven TextMeshPro counters, objective, progress and guidance |
| Solo development | Separate RIMBARA project; no Persons B/C invited |

## Day 0 foundation

- Separate Unity project and repository; the earlier Forest Reborn is untouched.
- Versioned Unity/package settings, Git ignore rules, Git LFS and asset metadata.
- Organized player, interaction, mission, UI, editor, scene and prefab folders.
- Editable Main, profiles, prefab instances, terrain, materials, effects and UI.
- Documented source-asset licences, play/build commands and repeatable tests.

## Deliberately excluded

Open world, realistic fire simulation, weather/seasons/day-night, animal AI,
multiplayer, cloud saves/leaderboards, complex inventory, crafting and skill
trees are outside this MVP. Static lighting, stylized effects, a small authored
map and a simple seedling counter are intentional.

Only master volume persists between launches. Replay and return-to-menu reload
the session and its world objects. A mission-save system is not included.

The presentation's **10–15-minute session target** needs human playtesting.
Automated runs deliberately sprint, use isolated fixtures for boundary tests,
and cannot establish a human session duration.
