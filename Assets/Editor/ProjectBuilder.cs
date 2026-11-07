using System;
using System.IO;
using System.Linq;
using Rimbara.Audio;
using Rimbara.Interaction;
using Rimbara.Mission;
using Rimbara.Player;
using Rimbara.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rimbara.Editor
{
    public static class ProjectBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string FullProfilePath = "Assets/Settings/ArunikaMVP.asset";
        public const string WeekOneProfilePath = "Assets/Settings/WeekOne.asset";
        private static string Root => AssetCatalog.Root;
        private static readonly Vector2[] SeedPositions = { new Vector2(-12f, 4f), new Vector2(-17f, 13f),
            new Vector2(-12f, 23f), new Vector2(12f, 7f), new Vector2(16f, 22f) };
        private static readonly Vector2[] BedPositions = { new Vector2(-7f, 9f), new Vector2(6f, 15f),
            new Vector2(-9f, 21f), new Vector2(9f, 27f), new Vector2(0f, 31f) };
        private static readonly Vector2[] FirePositions = { new Vector2(-8f, 49f), new Vector2(11f, 58f) };

        [MenuItem("RIMBARA/Regenerate authored Main scene")]
        private static void SetupFromMenu()
        {
            if (File.Exists(Path.Combine(Root, ScenePath)) && !EditorUtility.DisplayDialog("Regenerate RIMBARA?",
                "This replaces Main and the generated volunteer/interaction prefabs. Save a copy first if you have edited them.", "Regenerate", "Cancel")) return;
            SetupScene();
        }

        // Explicit generator for the first scene and reproducible development.
        // BuildWindows never regenerates an already-authored Main scene.
        public static void SetupScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before generating Main.");
            AssetCatalog.EnsureResources();
            ConfigureProject();
            MissionProfile full = Profile(FullProfilePath, 5, 5, 2, "Arunika / Full MVP");
            MissionProfile week = Profile(WeekOneProfilePath, 1, 1, 1, "Arunika / Week 1");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnvironmentFactory.ConfigureRendering();
            Transform world = Group("Arunika - authored restoration map");
            EnvironmentFactory.CreateTerrain(world);
            EnvironmentFactory.Populate(Group("Forest edge and static scenery", world));
            Transform zoneA = Group("Zone A - volunteer post and damaged forest", world);
            Transform zoneB = Group("Zone B - seedling nursery and planting clearings", world);
            Transform zoneC = Group("Zone C - remaining fires", world);

            var systems = new GameObject("Game systems - profiles, missions and flow");
            GameState state = systems.AddComponent<GameState>();
            state.Configure(full);
            MissionSystem missions = systems.AddComponent<MissionSystem>();
            Transform post = Anchor("Volunteer post return zone", zoneA, EnvironmentFactory.Ground(0f, -24f));
            Transform inspection = Anchor("Inspection marker", zoneA, EnvironmentFactory.Ground(0f, -7f));
            Transform planting = Anchor("Planting zone destination", zoneB, EnvironmentFactory.Ground(0f, 16f));
            Transform fireZone = Anchor("Fire zone destination", zoneC, EnvironmentFactory.Ground(0f, 49f));
            missions.Configure(state, inspection, post, planting, fireZone);
            systems.AddComponent<ProfileSelector>().Configure(state, full, week);
            CreateZone(post, missions, MissionZone.ZonePurpose.VolunteerPost, 3.8f);
            CreateZone(inspection, missions, MissionZone.ZonePurpose.Inspection, 3f);
            GameObject inspectionBeacon = EnvironmentFactory.Beacon("INSPECT THE FOREST", inspection, EnvironmentFactory.Amber, 2.1f);
            GameObject postBeacon = EnvironmentFactory.Beacon("RETURN TO VOLUNTEER POST", post, EnvironmentFactory.Amber, 2.8f);
            EnvironmentFactory.Ring("Inspection footprint", inspection, 2.3f,
                AssetCatalog.Material("Amber marker", EnvironmentFactory.Amber, true));
            CreatePost(zoneA);
            ZoneSign("A / DAMAGED FOREST", zoneA, -17f, -9f, EnvironmentFactory.Amber);
            ZoneSign("B / NEW ROOTS", zoneB, -22f, 15f, EnvironmentFactory.Green);
            ZoneSign("C / LAST FLAMES", zoneC, -19f, 43f, EnvironmentFactory.Amber);

            Camera camera = CreateCameras(out CinemachineCamera gameplay, out CinemachineCamera overview);
            GameObject player = EnvironmentFactory.Instance(CharacterFactory.CreateVolunteer(), null,
                EnvironmentFactory.Ground(0f, -21f) + Vector3.up * 0.10f);
            VolunteerRig rig = player.GetComponent<VolunteerRig>();
            VolunteerInput input = player.GetComponent<VolunteerInput>();
            VolunteerActor actor = player.GetComponent<VolunteerActor>();
            actor.Configure(state, missions);
            rig.ConfigureScene(camera, gameplay, overview, player.transform.position);
            gameplay.Follow = rig.CameraTarget;
            WaterSprayer water = player.GetComponent<WaterSprayer>();
            water.Configure(input, actor, camera, player.transform.Find("Water nozzle"),
                player.GetComponentInChildren<ParticleSystem>(), (1 << 8) | (1 << 11), 1 << 8);
            var flow = systems.AddComponent<GameFlow>();
            flow.Configure(state, missions, rig, input);

            GameObject tree = CreateRestoredTree();
            GameObject seedPrefab = CreateSeedlingPrefab();
            GameObject bedPrefab = CreateBedPrefab(tree);
            GameObject firePrefab = CreateFirePrefab();
            var seeds = new SeedlingPickup[5];
            var beds = new PlantingPoint[5];
            var fires = new FirePoint[2];
            var seedBeacons = new GameObject[5];
            var bedBeacons = new GameObject[5];
            var fireBeacons = new GameObject[2];
            for (int i = 0; i < 5; i++)
            {
                Vector2 seedPosition = SeedPositions[i];
                GameObject seed = EnvironmentFactory.Instance(seedPrefab, zoneB, EnvironmentFactory.Ground(seedPosition.x, seedPosition.y), i * 49f);
                seed.name = "Native seedling " + (i + 1).ToString("00");
                seeds[i] = seed.GetComponent<SeedlingPickup>();
                seeds[i].Configure("seed-" + (i + 1).ToString("00"), seed.transform.Find("Seedling visual").gameObject,
                    seed.transform.Find("Focus ring").gameObject, i);
                seedBeacons[i] = seed.transform.Find("Beacon - COLLECT SEEDLING").gameObject;
                Vector2 bedPosition = BedPositions[i];
                GameObject bed = EnvironmentFactory.Instance(bedPrefab, zoneB, EnvironmentFactory.Ground(bedPosition.x, bedPosition.y));
                bed.name = "Planting bed " + (i + 1).ToString("00");
                beds[i] = bed.GetComponent<PlantingPoint>();
                beds[i].Configure("bed-" + (i + 1).ToString("00"), tree, bed.transform.Find("Empty bed").gameObject,
                    bed.transform.Find("Focus ring").gameObject, i);
                bedBeacons[i] = bed.transform.Find("Beacon - PLANT A TREE").gameObject;
            }
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = FirePositions[i];
                GameObject fire = EnvironmentFactory.Instance(firePrefab, zoneC, EnvironmentFactory.Ground(position.x, position.y));
                fire.name = "Remaining fire " + (i + 1).ToString("00");
                fires[i] = fire.GetComponent<FirePoint>();
                fires[i].Configure("fire-" + (i + 1).ToString("00"), i, state, missions,
                    new[] { fire.transform.Find("Flames").GetComponent<ParticleSystem>(), fire.transform.Find("Smoke").GetComponent<ParticleSystem>() },
                    fire.GetComponentInChildren<Light>(), fire.transform.Find("Steam after suppression").gameObject);
                fires[i].ConfigureAudio(fire.GetComponent<AudioSource>());
                fireBeacons[i] = fire.transform.Find("Beacon - REMAINING FIRE").gameObject;
            }
            WorldGuide guide = systems.AddComponent<WorldGuide>();
            guide.Configure(state, missions, player.transform, seeds, beds, fires, inspectionBeacon, postBeacon, seedBeacons, bedBeacons);
            guide.ConfigureFireMarkers(fireBeacons);
            GameAudio sounds = CreateAudio(state, missions, water);
            HudReferences hud = HudFactory.Create(AssetCatalog.Font, AssetCatalog.Rounded, AssetCatalog.Circle);
            hud.Canvas.gameObject.AddComponent<RimbaraHud>().Configure(hud, state, missions, flow, actor,
                player.GetComponent<InteractionDetector>(), water, guide, rig, sounds);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = systems;
            WriteArtifact("scene-generation.json", JsonUtility.ToJson(new SceneSummary
            {
                Unity = Application.unityVersion, Scene = ScenePath, Seedlings = seeds.Length, Trees = beds.Length,
                Fires = fires.Length, Zones = 3, OfficialController = rig.OfficialController.GetType().FullName
            }, true));
            Debug.Log("RIMBARA Main generated: 3 zones, 4 ordered missions, 5 seedlings, 5 trees and 2 fires.");
        }

        [MenuItem("RIMBARA/Verify authored project")]
        public static void VerifyProject()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before structural verification.");
            if (!File.Exists(Path.Combine(Root, ScenePath))) throw new InvalidOperationException("Generate Main first.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ProjectVerification.Run();
        }

        [MenuItem("RIMBARA/Build Windows x64")]
        public static void BuildWindows()
        {
            if (!File.Exists(Path.Combine(Root, ScenePath))) SetupScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            VerifyProject();
            string output = Path.Combine(Root, "Builds", "Windows", "RIMBARA.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            var summary = new BuildSummary
            {
                Unity = Application.unityVersion, Result = report.summary.result.ToString(), Executable = output,
                Bytes = (long)report.summary.totalSize, Seconds = report.summary.totalTime.TotalSeconds,
                Warnings = (int)report.summary.totalWarnings, Errors = (int)report.summary.totalErrors
            };
            WriteArtifact("build-report.json", JsonUtility.ToJson(summary, true));
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("RIMBARA Windows build failed; see Artifacts/build-report.json and the Editor log.");
            CopyDistributionNotices(Path.GetDirectoryName(output));
            Debug.Log("RIMBARA Windows build ready: " + output);
        }

        private static void CopyDistributionNotices(string output)
        {
            File.Copy(Path.Combine(Root, "THIRD_PARTY_NOTICES.md"), Path.Combine(output, "THIRD_PARTY_NOTICES.md"), true);
            string licences = Path.Combine(output, "Licenses");
            Directory.CreateDirectory(licences);
            File.Copy(Path.Combine(Root, "Docs", "Licenses", "Unity-Companion-License.txt"),
                Path.Combine(licences, "Unity-Companion-License.txt"), true);
            File.Copy(Path.Combine(Root, "Assets", "Art", "Environment", "KenneyNature", "License.txt"),
                Path.Combine(licences, "Kenney-Nature-CC0.txt"), true);
            string fonts = Path.Combine(Root, "Assets", "TextMesh Pro", "Fonts");
            if (Directory.Exists(fonts))
                foreach (string fontNotice in Directory.GetFiles(fonts, "*.txt"))
                    File.Copy(fontNotice, Path.Combine(licences, Path.GetFileName(fontNotice)), true);
        }

        public static void WriteArtifact(string filename, string contents)
        {
            Directory.CreateDirectory(Path.Combine(Root, "Artifacts"));
            File.WriteAllText(Path.Combine(Root, "Artifacts", filename), contents);
        }

        private static void ConfigureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "Rimbara";
            PlayerSettings.productName = "RIMBARA";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tags.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "World";
            layers.GetArrayElementAtIndex(9).stringValue = "Volunteer";
            layers.GetArrayElementAtIndex(10).stringValue = "Interaction";
            layers.GetArrayElementAtIndex(11).stringValue = "Fire";
            layers.GetArrayElementAtIndex(12).stringValue = "MissionZone";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MissionProfile Profile(string path, int seedlings, int trees, int fires, string name)
        {
            MissionProfile profile = AssetDatabase.LoadAssetAtPath<MissionProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<MissionProfile>();
            profile.Seedlings = seedlings;
            profile.Trees = trees;
            profile.Fires = fires;
            profile.DisplayName = name;
            profile.ExtinguishSeconds = 2.4f;
            profile.SprayRange = 7f;
            profile.InteractionRange = 2.5f;
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static Transform Group(string name, Transform parent = null)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static Transform Anchor(string name, Transform parent, Vector3 position)
        {
            Transform root = Group(name, parent);
            root.position = position;
            return root;
        }

        private static void CreateZone(Transform anchor, MissionSystem missions, MissionZone.ZonePurpose purpose, float radius)
        {
            anchor.gameObject.layer = 12;
            SphereCollider trigger = anchor.gameObject.AddComponent<SphereCollider>();
            trigger.radius = radius;
            trigger.center = Vector3.up;
            trigger.isTrigger = true;
            anchor.gameObject.AddComponent<MissionZone>().Configure(missions, purpose);
        }

        private static void CreatePost(Transform zone)
        {
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("tent_detailedOpen", 2.9f), zone, EnvironmentFactory.Ground(-7f, -25f), 25f);
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("log_stack", 0.8f), zone, EnvironmentFactory.Ground(7f, -27f), 90f);
            Transform board = Anchor("Volunteer mission board", zone, EnvironmentFactory.Ground(0f, -27f));
            board.gameObject.layer = 10;
            Material wood = AssetCatalog.Material("Post timber", new Color(0.42f, 0.30f, 0.17f));
            Material face = AssetCatalog.Material("Post sign face", new Color(0.12f, 0.29f, 0.18f));
            EnvironmentFactory.Shape(PrimitiveType.Cube, "Board left post", board, new Vector3(-0.8f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), wood);
            EnvironmentFactory.Shape(PrimitiveType.Cube, "Board right post", board, new Vector3(0.8f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), wood);
            EnvironmentFactory.Shape(PrimitiveType.Cube, "Briefing board", board, new Vector3(0f, 1.45f, 0f), new Vector3(2.15f, 1.2f, 0.12f), face);
            BoxCollider collider = board.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.size = new Vector3(2.2f, 1.9f, 0.35f);
            GameObject ring = EnvironmentFactory.Ring("Board focus ring", board, 1.6f,
                AssetCatalog.Material("Amber marker", EnvironmentFactory.Amber, true));
            board.gameObject.AddComponent<MissionBoard>().SetIdentity("volunteer-board", ring);
            EnvironmentFactory.WorldText("ARUNIKA\nVOLUNTEER POST", board, new Vector3(0f, 1.5f, 0.10f),
                new Color(0.90f, 0.95f, 0.83f), 2.5f, 2.2f);
        }

        private static void ZoneSign(string text, Transform parent, float x, float z, Color color)
        {
            Transform root = Anchor(text, parent, EnvironmentFactory.Ground(x, z));
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("sign", 1.4f), root, root.position, 15f);
            EnvironmentFactory.WorldText(text, root, Vector3.up * 2f, color, 3f, 8f);
        }

        private static Camera CreateCameras(out CinemachineCamera gameplay, out CinemachineCamera overview)
        {
            var root = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
            root.tag = "MainCamera";
            Camera camera = root.GetComponent<Camera>();
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 220f;
            camera.fieldOfView = 57f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            CinemachineBrain brain = root.GetComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.7f);
            gameplay = new GameObject("Cinemachine - volunteer third-person").AddComponent<CinemachineCamera>();
            gameplay.Lens.FieldOfView = 57f;
            gameplay.Lens.NearClipPlane = 0.08f;
            gameplay.Lens.FarClipPlane = 220f;
            CinemachineThirdPersonFollow follow = gameplay.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.50f, -0.08f, 0f);
            follow.VerticalArmLength = 0.35f;
            follow.CameraDistance = 4.2f;
            follow.CameraSide = 1f;
            follow.Damping = new Vector3(0.10f, 0.20f, 0.12f);
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true, CollisionFilter = 1 << 8, IgnoreTag = "Player", CameraRadius = 0.18f,
                DampingIntoCollision = 0f, DampingFromCollision = 0.45f
            };
            overview = new GameObject("Cinemachine - Arunika overview").AddComponent<CinemachineCamera>();
            overview.transform.position = new Vector3(29f, 23f, -9f);
            overview.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 24f) - overview.transform.position);
            overview.Lens.FieldOfView = 56f;
            overview.Lens.NearClipPlane = 0.1f;
            overview.Lens.FarClipPlane = 220f;
            root.transform.SetPositionAndRotation(overview.transform.position, overview.transform.rotation);
            return camera;
        }

        private static GameObject CreateRestoredTree()
        {
            const string path = "Assets/Prefabs/Environment/RestoredNativeTree.prefab";
            Transform root = Group("Restored native tree and ground cover");
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("tree_small", 4.4f), root, Vector3.zero);
            EnvironmentFactory.Shape(PrimitiveType.Cylinder, "Fresh growth patch", root, Vector3.up * 0.02f,
                new Vector3(2.4f, 0.02f, 2.4f), AssetCatalog.Material("Fresh ground cover", new Color(0.31f, 0.45f, 0.20f)));
            GameObject grass = AssetCatalog.NaturePrefab("grass_leafs", 0.32f);
            GameObject flower = AssetCatalog.NaturePrefab("flower_yellowA", 0.28f);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 0.4f;
                EnvironmentFactory.Instance(i % 2 == 0 ? grass : flower, root,
                    new Vector3(Mathf.Cos(angle), 0.04f, Mathf.Sin(angle)) * 0.8f, i * 67f);
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static GameObject CreateSeedlingPrefab()
        {
            Transform root = Group("Native seedling");
            root.gameObject.layer = 10;
            Transform visual = Group("Seedling visual", root);
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("pot_small", 0.38f), visual, Vector3.zero);
            GameObject seedling = EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("tree_small", 0.72f), visual, Vector3.up * 0.30f);
            foreach (Collider collider in seedling.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            BoxCollider target = root.gameObject.AddComponent<BoxCollider>();
            target.isTrigger = true;
            target.center = Vector3.up * 0.55f;
            target.size = new Vector3(1.1f, 1.2f, 1.1f);
            GameObject ring = EnvironmentFactory.Ring("Focus ring", root, 0.75f,
                AssetCatalog.Material("Green marker", EnvironmentFactory.Green, true));
            EnvironmentFactory.Beacon("COLLECT SEEDLING", root, EnvironmentFactory.Green, 1.65f);
            root.gameObject.AddComponent<SeedlingPickup>().Configure("seed-template", visual.gameObject, ring, 0);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/Interaction/NativeSeedling.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static GameObject CreateBedPrefab(GameObject tree)
        {
            Transform root = Group("Planting bed");
            root.gameObject.layer = 10;
            GameObject soil = EnvironmentFactory.Shape(PrimitiveType.Cylinder, "Empty bed", root, Vector3.up * 0.04f,
                new Vector3(1.7f, 0.04f, 1.7f), AssetCatalog.Material("Planting soil", new Color(0.27f, 0.20f, 0.13f)));
            GameObject ring = EnvironmentFactory.Ring("Focus ring", root, 1.2f,
                AssetCatalog.Material("Green marker", EnvironmentFactory.Green, true));
            BoxCollider target = root.gameObject.AddComponent<BoxCollider>();
            target.isTrigger = true;
            target.center = Vector3.up * 0.35f;
            target.size = new Vector3(1.8f, 0.7f, 1.8f);
            EnvironmentFactory.Beacon("PLANT A TREE", root, EnvironmentFactory.Green, 1.15f);
            root.gameObject.AddComponent<PlantingPoint>().Configure("bed-template", tree, soil, ring, 0);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/Interaction/PlantingBed.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static GameObject CreateFirePrefab()
        {
            Transform root = Group("Remaining fire");
            root.gameObject.layer = 11;
            EnvironmentFactory.Instance(AssetCatalog.NaturePrefab("campfire_logs", 0.55f), root, Vector3.zero, 15f);
            ParticleSystem flame = ParticleFactory.Flame(root);
            ParticleSystem smoke = ParticleFactory.Smoke(root);
            GameObject steam = ParticleFactory.Steam(root);
            var glowObject = new GameObject("Fire glow");
            glowObject.transform.SetParent(root, false);
            glowObject.transform.localPosition = Vector3.up * 1.1f;
            Light glow = glowObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.43f, 0.09f);
            glow.intensity = 1.8f;
            glow.range = 6.5f;
            glow.shadows = LightShadows.None;
            SphereCollider target = root.gameObject.AddComponent<SphereCollider>();
            target.isTrigger = true;
            target.center = Vector3.up * 1.1f;
            target.radius = 1.05f;
            EnvironmentFactory.Beacon("REMAINING FIRE", root, EnvironmentFactory.Amber, 2.7f);
            FirePoint fire = root.gameObject.AddComponent<FirePoint>();
            fire.Configure("fire-template", 0, null, null, new[] { flame, smoke }, glow, steam);
            AudioSource source = AudioSource(root.gameObject, AudioFactory.Clip("FireCrackle"), true, 0.45f, true);
            fire.ConfigureAudio(source);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/Interaction/RemainingFire.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static GameAudio CreateAudio(GameState state, MissionSystem missions, WaterSprayer water)
        {
            Transform root = Group("Audio - original procedural soundscape");
            AudioSource wind = AudioSource(Group("Forest wind", root).gameObject, AudioFactory.Clip("ForestWind"), true, 0.25f);
            AudioSource birds = AudioSource(Group("Returning birds", root).gameObject, AudioFactory.Clip("ForestBirds"), true, 0.04f);
            AudioSource spray = AudioSource(Group("Water tool loop", root).gameObject, AudioFactory.Clip("WaterSpray"), true, 0.42f);
            spray.playOnAwake = false;
            AudioSource feedback = AudioSource(Group("Mission feedback", root).gameObject, null, false, 0.85f);
            feedback.playOnAwake = false;
            GameAudio system = root.gameObject.AddComponent<GameAudio>();
            system.Configure(state, missions, water, wind, birds, spray, feedback,
                AudioFactory.Clip("CollectSeedling"), AudioFactory.Clip("PlantTree"), AudioFactory.Clip("FireOut"), AudioFactory.Clip("RestorationComplete"));
            return system;
        }

        private static AudioSource AudioSource(GameObject root, AudioClip clip, bool loop, float volume, bool spatial = false)
        {
            var source = root.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.playOnAwake = clip != null;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 22f;
            return source;
        }

        [Serializable]
        private sealed class SceneSummary
        {
            public string Unity;
            public string Scene;
            public int Seedlings;
            public int Trees;
            public int Fires;
            public int Zones;
            public string OfficialController;
        }

        [Serializable]
        private sealed class BuildSummary
        {
            public string Unity;
            public string Result;
            public string Executable;
            public long Bytes;
            public double Seconds;
            public int Warnings;
            public int Errors;
        }
    }
}
