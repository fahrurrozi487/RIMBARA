# Third-party assets and licences

This private Unity project includes both CC0 and licensed assets. No blanket
open-source licence is applied to all repository content. Third-party rights
remain with their respective owners.

## Unity Starter Assets

- **First Person + Third Person | Character Controllers**, product **196526**,
  version **2.0.2**, by **Unity Technologies**.
- Acquired through the owner's Unity My Assets library and Unity's authenticated
  Asset Store downloader; not sourced from an unofficial GitHub copy.
- Source/assets are preserved under `Assets/Starter Assets`.
- Licence shown by the official listing: **Unity Companion License**.
- The current official licence text is included in
  `Docs/Licenses/Unity-Companion-License.txt` (v1.4, dated 2024-10-29).
- Copyright Unity Technologies; trademarks remain their owners' property.
- [Official listing](https://assetstore.unity.com/packages/3d/characters/first-person-third-person-character-controllers-196526)
- [Official licence](https://unity.com/legal/licenses/unity-companion-license)

## Mixamo / Adobe

- **Y Bot**, **Idle**, **Walking (Male Standard Walk)**, and **Running** from
  [mixamo.com](https://www.mixamo.com/).
- Imported source FBX files are under `Assets/Art/Characters/Mixamo`.
- Walking and running were exported in place, without skin, as FBX for Unity,
  30 FPS, with no keyframe reduction. Y Bot supplies the character skin/avatar.
- These are licensed Mixamo assets, **not open-source or CC0 assets**.
  Use them as part of the game under Adobe's applicable terms. Do not publish
  the raw character/animation files as a standalone redistributable asset pack.
- [Adobe Mixamo FAQ and licensing information](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)

## Kenney Nature Kit

- **Nature Kit 2.1**, created/distributed by **Kenney**.
- 32 selected FBX source meshes are under
  `Assets/Art/Environment/KenneyNature`; project-prefab variants/materials are
  generated from those meshes.
- Licence: **Creative Commons Zero (CC0)**. The original licence notice is
  preserved as `Assets/Art/Environment/KenneyNature/License.txt`.
- [Official asset page](https://kenney.nl/assets/nature-kit)
- [CC0](https://creativecommons.org/publicdomain/zero/1.0/)

## Unity packages and TextMeshPro resources

URP, Cinemachine, Input System, uGUI/TextMeshPro, Test Framework, and their
dependencies are resolved through Unity Package Manager. Their package
licences and notices remain authoritative. Package caches are not committed.
TextMeshPro essential resources include their supplied font notices/licences,
which are preserved in the imported `Assets/TextMesh Pro` content.

## Original project content

RIMBARA's gameplay scripts, mission/profile setup, scene/UI/effect generators,
and procedural WAV audio are original project content. Their presence here
does not relicense any third-party code, mesh, animation, font, or texture.

Before making this repository public or redistributing source separately,
review the relevant asset terms or replace assets whose source cannot be
redistributed in the intended manner.
