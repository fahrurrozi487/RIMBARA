using System;
using System.IO;
using Rimbara.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rimbara.Editor
{
    internal static class EnvironmentFactory
    {
        public static readonly Color Green = new Color(0.42f, 0.82f, 0.39f);
        public static readonly Color Amber = new Color(1f, 0.73f, 0.25f);
        public static Terrain Terrain { get; private set; }

        public static void ConfigureRendering()
        {
            const string rendererPath = "Assets/Settings/ArunikaRenderer.asset";
            const string pipelinePath = "Assets/Settings/ArunikaURP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "Arunika Forward Renderer";
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Arunika URP";
                pipeline.msaaSampleCount = 4;
                pipeline.renderScale = 1f;
                pipeline.supportsHDR = true;
                pipeline.supportsCameraDepthTexture = true;
                pipeline.mainLightShadowmapResolution = 2048;
                pipeline.shadowDistance = 85f;
                pipeline.shadowCascadeCount = 2;
                pipeline.maxAdditionalLightsCount = 4;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previousQuality, false);
            QualitySettings.vSyncCount = 1;
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;

            GameObject sunObject = new GameObject("Sun - static late morning");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.91f, 0.72f);
            sun.intensity = 1.7f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.68f;
            sun.transform.rotation = Quaternion.Euler(49f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.61f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.59f, 0.43f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.60f, 0.69f, 0.61f);
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 135f;
            const string skyPath = "Assets/Art/Materials/ArunikaSky.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                sky.SetColor("_SkyTint", new Color(0.57f, 0.66f, 0.63f));
                sky.SetColor("_GroundColor", new Color(0.45f, 0.50f, 0.32f));
                sky.SetFloat("_AtmosphereThickness", 1.15f);
                sky.SetFloat("_Exposure", 1.05f);
                sky.SetFloat("_SunSize", 0.025f);
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            RenderSettings.skybox = sky;
            GameObject volumeObject = new GameObject("Atmosphere - static grading");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            const string volumePath = "Assets/Settings/ArunikaAtmosphere.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, volumePath);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.16f);
                bloom.threshold.Override(1.2f);
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.16f);
                vignette.smoothness.Override(0.45f);
                var grading = profile.Add<ColorAdjustments>(true);
                grading.saturation.Override(6f);
                grading.contrast.Override(6f);
                foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            }
            volume.sharedProfile = profile;
        }

        public static Terrain CreateTerrain(Transform parent)
        {
            const string path = "Assets/Art/Environment/ArunikaTerrain.asset";
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = new TerrainData { name = "Arunika restoration terrain", heightmapResolution = 129, size = new Vector3(128f, 14f, 128f), alphamapResolution = 128, baseMapResolution = 256 };
                var heights = new float[129, 129];
                for (int z = 0; z < 129; z++)
                {
                    for (int x = 0; x < 129; x++)
                    {
                        float worldX = x - 64f;
                        float worldZ = z - 40f;
                        float edge = Mathf.Max(Mathf.InverseLerp(39f, 63f, Mathf.Abs(worldX)),
                            Mathf.Max(Mathf.InverseLerp(-27f, -40f, worldZ), Mathf.InverseLerp(75f, 88f, worldZ)));
                        float noise = Mathf.PerlinNoise(x * 0.047f + 7.8f, z * 0.047f + 12.3f);
                        heights[z, x] = (0.055f + noise * 0.05f + edge * edge * (2.7f + noise * 3.8f)) / 14f;
                    }
                }
                data.SetHeights(0, 0, heights);
                TerrainLayer dry = Layer("DryEarth", new Color(0.52f, 0.40f, 0.23f));
                TerrainLayer moss = Layer("NewGrowth", new Color(0.33f, 0.49f, 0.24f));
                TerrainLayer burnt = Layer("BurntEarth", new Color(0.24f, 0.24f, 0.18f));
                data.terrainLayers = new[] { dry, moss, burnt };
                var paint = new float[128, 128, 3];
                for (int z = 0; z < 128; z++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float worldX = x - 64f;
                        float worldZ = z - 40f;
                        float growth = Mathf.Clamp01((Mathf.Abs(worldX) - 24f) / 25f) * 0.75f;
                        if (worldZ > 2f && worldZ < 31f && Mathf.Abs(worldX) < 25f) growth = Mathf.Max(growth, 0.18f);
                        float charred = worldZ > 37f ? Mathf.Clamp01((worldZ - 37f) / 10f) * 0.7f : 0f;
                        growth *= 1f - charred;
                        paint[z, x, 0] = Mathf.Max(0f, 1f - growth - charred);
                        paint[z, x, 1] = growth;
                        paint[z, x, 2] = charred;
                    }
                }
                data.SetAlphamaps(0, 0, paint);
                AssetDatabase.CreateAsset(data, path);
            }
            GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Arunika terrain - paintable in the Inspector";
            terrainObject.transform.SetParent(parent);
            terrainObject.transform.position = new Vector3(-64f, -0.04f, -40f);
            terrainObject.layer = 8;
            Terrain = terrainObject.GetComponent<Terrain>();
            Terrain.drawInstanced = true;
            Terrain.heightmapPixelError = 4f;
            Terrain.basemapDistance = 160f;
            Terrain.detailObjectDistance = 0f;
            Terrain.treeDistance = 0f;
            const string terrainMaterialPath = "Assets/Art/Materials/ArunikaTerrain.mat";
            Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(terrainMaterialPath);
            if (terrainMaterial == null)
            {
                terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                AssetDatabase.CreateAsset(terrainMaterial, terrainMaterialPath);
            }
            Terrain.materialTemplate = terrainMaterial;
            return Terrain;
        }

        private static TerrainLayer Layer(string name, Color color)
        {
            string path = AssetCatalog.Environment + "/" + name + ".terrainlayer";
            TerrainLayer existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (existing != null) return existing;
            string texturePath = AssetCatalog.Art + "/Textures/" + name + ".png";
            Texture2D texture = AssetCatalog.EnsureTexture(texturePath, (x, y) =>
            {
                float grain = Mathf.PerlinNoise(x * 0.2f + 2.6f, y * 0.2f + 8.4f) * 0.18f - 0.09f;
                return new Color(color.r + grain, color.g + grain, color.b + grain, 1f);
            });
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            var layer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(5f, 5f), smoothness = 0f };
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }

        public static Vector3 Ground(float x, float z)
        {
            float height = Terrain == null ? 0f : Terrain.SampleHeight(new Vector3(x, 0f, z)) + Terrain.transform.position.y;
            return new Vector3(x, height, z);
        }

        public static GameObject Instance(GameObject prefab, Transform parent, Vector3 position, float yaw = 0f)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return instance;
        }

        public static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material,
            bool collider = false, int layer = 8)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.layer = layer;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            return shape;
        }

        public static GameObject Ring(string name, Transform parent, float radius, Material material)
        {
            var ring = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = Vector3.up * 0.06f;
            ring.transform.localScale = new Vector3(radius, 1f, radius);
            ring.layer = 2;
            ring.GetComponent<MeshFilter>().sharedMesh = AssetCatalog.RingMesh();
            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ring;
        }

        public static GameObject Beacon(string label, Transform parent, Color color, float height = 1.7f)
        {
            var root = new GameObject("Beacon - " + label);
            root.transform.SetParent(parent, false);
            Material glow = AssetCatalog.Material(color == Amber ? "Amber marker" : "Green marker", color, true);
            GameObject diamond = Shape(PrimitiveType.Cube, "Floating diamond", root.transform, new Vector3(0f, height, 0f), Vector3.one * 0.25f, glow, false, 2);
            diamond.transform.localRotation = Quaternion.Euler(35f, 45f, 45f);
            diamond.AddComponent<BeaconPulse>();
            WorldText(label, root.transform, new Vector3(0f, height + 0.45f, 0f), color, 2.6f, 7f);
            return root;
        }

        public static TMP_Text WorldText(string text, Transform parent, Vector3 position, Color color, float size, float width)
        {
            var root = new GameObject("Label - " + text, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.layer = 2;
            var label = root.AddComponent<TextMeshPro>();
            label.font = AssetCatalog.Font;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(width, 1.5f);
            label.renderer.shadowCastingMode = ShadowCastingMode.Off;
            root.AddComponent<Billboard>();
            return label;
        }

        public static void Populate(Transform root)
        {
            var random = new System.Random(71029);
            GameObject[] trees =
            {
                AssetCatalog.NaturePrefab("tree_default", 7.7f), AssetCatalog.NaturePrefab("tree_detailed", 7.1f),
                AssetCatalog.NaturePrefab("tree_oak", 8.4f), AssetCatalog.NaturePrefab("tree_pineRoundA", 8.3f),
                AssetCatalog.NaturePrefab("tree_pineTallA", 9.6f), AssetCatalog.NaturePrefab("tree_palmDetailedShort", 7.2f)
            };
            for (int i = 0; i < 94; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (34f + (float)random.NextDouble() * 22f);
                float z = -33f + (float)random.NextDouble() * 111f;
                GameObject tree = Instance(trees[i % trees.Length], root, Ground(x, z), (float)random.NextDouble() * 360f);
                tree.transform.localScale *= 0.75f + (float)random.NextDouble() * 0.40f;
                tree.isStatic = true;
            }
            for (int i = 0; i < 20; i++)
            {
                float x = -57f + i * 6f;
                GameObject tree = Instance(trees[i % trees.Length], root, Ground(x, i % 2 == 0 ? -34f : 79f), i * 67f);
                tree.isStatic = true;
            }
            GameObject[] rocks = { AssetCatalog.NaturePrefab("rock_largeA", 1.8f), AssetCatalog.NaturePrefab("rock_largeB", 2.6f), AssetCatalog.NaturePrefab("rock_tallA", 3.4f) };
            for (int i = 0; i < 32; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (29f + (float)random.NextDouble() * 22f);
                float z = -29f + (float)random.NextDouble() * 104f;
                Instance(rocks[i % rocks.Length], root, Ground(x, z), i * 43f).isStatic = true;
            }
            GameObject bush = AssetCatalog.NaturePrefab("plant_bushDetailed", 0.75f);
            GameObject grass = AssetCatalog.NaturePrefab("grass_large", 0.36f);
            for (int i = 0; i < 58; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (25f + (float)random.NextDouble() * 29f);
                float z = -32f + (float)random.NextDouble() * 110f;
                Instance(i % 3 == 0 ? bush : grass, root, Ground(x, z), i * 63f).isStatic = true;
            }
            GameObject stump = AssetCatalog.NaturePrefab("stump_roundDetailed", 0.75f);
            Vector2[] stumps = { new Vector2(-13f, -21f), new Vector2(13f, -18f), new Vector2(-18f, -3f), new Vector2(21f, 1f), new Vector2(-24f, 18f), new Vector2(20f, 32f), new Vector2(-21f, 38f) };
            foreach (Vector2 point in stumps) Instance(stump, root, Ground(point.x, point.y), point.x * 13f).isStatic = true;
            Material burnt = AssetCatalog.Material("Charred bark", new Color(0.15f, 0.14f, 0.105f));
            for (int i = 0; i < 11; i++)
            {
                Vector3 position = Ground((i % 2 == 0 ? -1f : 1f) * (19f + (i % 3) * 4f), 40f + i * 3.1f);
                var dead = new GameObject("Charred tree " + (i + 1));
                dead.transform.SetParent(root, false);
                dead.transform.position = position;
                Shape(PrimitiveType.Cylinder, "Burnt trunk", dead.transform, new Vector3(0f, 2.2f, 0f), new Vector3(0.36f, 2.2f, 0.36f), burnt, true);
                for (int branch = 0; branch < 3; branch++)
                {
                    GameObject limb = Shape(PrimitiveType.Cylinder, "Bare branch", dead.transform, new Vector3((branch % 2 == 0 ? -1f : 1f) * 0.6f, 2.5f + branch * 0.5f, 0.1f), new Vector3(0.17f, 0.75f, 0.17f), burnt);
                    limb.transform.localRotation = Quaternion.Euler(15f, branch * 79f, branch % 2 == 0 ? 42f : -42f);
                }
                dead.isStatic = true;
            }
            Material trail = AssetCatalog.Material("Path stones", new Color(0.63f, 0.57f, 0.40f));
            GameObject stone = AssetCatalog.NaturePrefab("stone_smallFlatA", 0.1f);
            for (int i = 0; i < 31; i++)
            {
                float z = -19f + i * 2.65f;
                float x = Mathf.Sin(i * 0.25f) * 1.5f;
                GameObject tile = Instance(stone, root, Ground(x, z), i * 38f);
                tile.transform.localScale *= 1.1f;
                foreach (Renderer renderer in tile.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = trail;
                foreach (Collider collider in tile.GetComponentsInChildren<Collider>()) collider.enabled = false;
                tile.isStatic = true;
            }
            // Invisible edge rails prevent leaving the small authored map. If a
            // developer removes these, VolunteerRig still recovers falls safely.
            Shape(PrimitiveType.Cube, "West map boundary", root, new Vector3(-61f, 4f, 22f), new Vector3(2f, 16f, 130f), burnt, true).GetComponent<Renderer>().enabled = false;
            Shape(PrimitiveType.Cube, "East map boundary", root, new Vector3(61f, 4f, 22f), new Vector3(2f, 16f, 130f), burnt, true).GetComponent<Renderer>().enabled = false;
            Shape(PrimitiveType.Cube, "South map boundary", root, new Vector3(0f, 4f, -38f), new Vector3(124f, 16f, 2f), burnt, true).GetComponent<Renderer>().enabled = false;
            Shape(PrimitiveType.Cube, "North map boundary", root, new Vector3(0f, 4f, 85f), new Vector3(124f, 16f, 2f), burnt, true).GetComponent<Renderer>().enabled = false;
        }
    }
}
