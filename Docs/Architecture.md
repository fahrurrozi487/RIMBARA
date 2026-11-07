# Project architecture

## Authored scene

`Assets/Scenes/Main.unity` is a normal, editable Unity scene, not a runtime-only
procedural world. Its hierarchy contains:

- **Arunika - authored restoration map**: paintable terrain, static scenery,
  and three named zones.
- **Zone A**: volunteer post/board, return trigger, and inspection trigger.
- **Zone B**: five unique native-seedling and five planting-bed prefab instances.
- **Zone C**: two unique fire prefab instances.
- **Volunteer**: Mixamo Y Bot, the unmodified official Starter Assets motor,
  CharacterController, Humanoid Animator, water pack/tool, and RIMBARA adapters.
- **Game systems**: profile selection, progress, ordered missions, game flow,
  and nearest-object guidance.
- **Main Camera** and Cinemachine gameplay/overview cameras.
- **UI - RIMBARA**, EventSystem, soundscape, directional light, and URP grading.

## Runtime folders

| Folder | Responsibility |
| --- | --- |
| `Assets/Scripts/Player` | Action map, official-motor adapter, camera/recovery, tool IK pose |
| `Assets/Scripts/Interaction` | Nearby detection, pickup, planting, mission board, water/fire |
| `Assets/Scripts/Mission` | Profiles, inventory/progress, mission sequence, zones, menu/pause/victory |
| `Assets/Scripts/UI` | TextMeshPro references and event-driven HUD |
| `Assets/Scripts/Audio` | Original soundscape and action feedback |
| `Assets/Scripts/Diagnostics` | Opt-in standalone input/gameplay smoke driver |
| `Assets/Scripts/World` | Billboard labels and beacon animation |
| `Assets/Editor` | Resource, character, effects, environment, HUD, scene, build and verification helpers |
| `Assets/Tests/EditMode` | Engine-independent automated model tests |

## Progress and events

`RestorationProgress` is a deterministic, engine-independent model. Separate
sets of unique object IDs prevent duplicate collection, planting, or fire
rewards. Planting requires inventory and consumes one carried seedling.
Counters cannot exceed profile goals.

`GameState` owns that model and broadcasts progress/action events. The HUD
subscribes to events rather than keeping a second set of gameplay counters.
Only nearby, unblocked interactables can be selected by `InteractionDetector`.

`MissionSequence` is the engine-independent ordered state machine;
`MissionSystem` connects it to session state and Unity triggers:

```text
FirstSteps --inspection--> SeedsOfHope --collection + planting goals-->
StopTheFire --fire goal--> ANewBeginning --return to post--> Complete
```

The seed/plant components and water tool enforce the active mission. Menu,
briefing, pause, and victory disable gameplay mutation. `GameFlow` freezes
scaled time during pause and reloads Main for replay/menu, so used object IDs
and visual objects reset together.

## Official controller integration

The vendor source remains in `Assets/Starter Assets`; Git preserves the original
vendor script bytes instead of normalizing their line endings. RIMBARA does not
replace its movement/gravity implementation. `VolunteerRig` sends RIMBARA's move,
look, sprint, and disabled-jump values into `StarterAssetsInputs`; the official
`ThirdPersonController` performs movement and rotates the camera target.

The original PlayerInput map is deactivated to avoid double input, while its
asset/control scheme remains assigned. The root Humanoid Animator contains
the Speed, MotionSpeed, Grounded, Jump, and FreeFall parameters expected by
the motor and blends Mixamo idle/walk/run clips. Cinemachine's third-person
follow handles world obstacles. Falling out of bounds teleports the existing
volunteer to the post without resetting counters.

## Water and restoration

The camera crosshair selects a fire through a raycast. Water applies only
within the profile range and with a clear nozzle-to-fire line. Contact time
accumulates in `FireSuppression`; stopping, pausing, or missing does not erase
progress. There is no water inventory or damage.

Each completed planting instantiates the editable native-tree/ground-cover
prefab. Each completed fire stops flame/smoke/audio, disables its target
collider, and plays a steam burst. Returning birds increase with restoration.

## Editing and regeneration

Profiles live in `Assets/Settings`; interaction, environment, player, and FX
prefabs live in their corresponding `Assets/Prefabs` subfolders. Terrain,
materials, textures, character Animator, UI, and audio remain project assets.

Normal editing/building uses Main directly. The generator is only for explicit
regeneration and overwrites generated scene/prefab content. It preserves
existing profile/material resource assets. If changing map supply, update
profile validation, unique IDs/slots, guide arrays, and verification together.
