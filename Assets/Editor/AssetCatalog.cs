using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rimbara.Editor
{
    internal static class AssetCatalog
    {
        public const string Art = "Assets/Art";
        public const string Materials = Art + "/Materials";
        public const string Environment = Art + "/Environment";
        public static TMP_FontAsset Font { get; private set; }
        public static Sprite Rounded { get; private set; }
        public static Sprite Circle { get; private set; }
        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static void EnsureResources()
        {
            foreach (string path in new[] { Materials, Environment, Art + "/Textures", "Assets/Audio", "Assets/Prefabs/Environment",
                "Assets/Prefabs/Interaction", "Assets/Prefabs/Player", "Assets/Prefabs/FX", "Assets/Settings", "Assets/Scenes", "Assets/UI" })
                Directory.CreateDirectory(Path.Combine(Root, path));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (Font == null)
            {
                // ImportPackage is asynchronous. Never start an import midway
                // through generation and then use resources that are not ready.
                // The essential resources are committed with the authored game.
                throw new InvalidOperationException("Import TMP Essential Resources via Window > TextMeshPro, "
                    + "wait for the import to finish, then regenerate Main.");
            }
            Rounded = EnsureSprite("Assets/UI/RoundedPanel.png", false);
            Circle = EnsureSprite("Assets/UI/Circle.png", true);
            EnsureTexture(Art + "/Textures/ParticleSoft.png", (x, y) =>
            {
                float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 1.5f));
            });
            AudioFactory.EnsureClips();
        }

        private static Sprite EnsureSprite(string path, bool circular)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null && HasOpaqueCenter(path)) return existing;
            EnsureTexture(path, (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f);
                float dy = Mathf.Abs(y - 31.5f);
                float distance = circular ? new Vector2(dx, dy).magnitude
                    : new Vector2(Mathf.Max(0f, dx - 17.5f), Mathf.Max(0f, dy - 17.5f)).magnitude;
                float edge = circular ? 30.5f : 14f;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 1.2f, edge, distance));
                return new Color(1f, 1f, 1f, alpha);
            }, true);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = circular ? Vector4.zero : Vector4.one * 17f;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static bool HasOpaqueCenter(string path)
        {
            string absolute = Path.Combine(Root, path);
            if (!File.Exists(absolute)) return false;
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                return image.LoadImage(File.ReadAllBytes(absolute)) && image.GetPixel(image.width / 2, image.height / 2).a > 0.95f;
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        public static Texture2D EnsureTexture(string path, Func<int, int, Color> color, bool rebuild = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && !rebuild) return existing;
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++) texture.SetPixel(x, y, color(x, y));
            texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Root, path)));
            File.WriteAllBytes(Path.Combine(Root, path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Materials + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("The Universal Render Pipeline package is required.");
            var material = new Material(shader) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            if (!unlit)
            {
                material.SetFloat("_Smoothness", 0.08f);
                material.SetFloat("_Metallic", 0f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static Material ParticleMaterial(string name, bool additive)
        {
            string path = Materials + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/ParticleSoft.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void ConvertMaterials(GameObject model)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                var converted = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    Material original = source[i];
                    Color color = original == null ? new Color(0.5f, 0.62f, 0.45f) : original.HasProperty("_BaseColor")
                        ? original.GetColor("_BaseColor") : original.HasProperty("_Color") ? original.color : Color.white;
                    color.a = 1f;
                    Color32 bytes = color;
                    string suffix = bytes.r.ToString("X2") + bytes.g.ToString("X2") + bytes.b.ToString("X2");
                    converted[i] = Material("Palette_" + suffix, color);
                }
                renderer.sharedMaterials = converted;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        public static GameObject NaturePrefab(string name, float height)
        {
            string size = height.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', '_');
            string path = "Assets/Prefabs/Environment/" + name + "_" + size + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Environment + "/KenneyNature/" + name + ".fbx");
            if (source == null) throw new FileNotFoundException("Missing the downloaded Kenney CC0 mesh: " + name);
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform, false);
            Bounds bounds = ModelBounds(model);
            model.transform.localScale = Vector3.one * (height / Mathf.Max(0.01f, bounds.size.y));
            bounds = ModelBounds(model);
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            ConvertMaterials(model);
            SetLayer(root, 8);
            if (name.StartsWith("tree", StringComparison.Ordinal))
            {
                CapsuleCollider trunk = root.AddComponent<CapsuleCollider>();
                trunk.center = new Vector3(0f, height * 0.45f, 0f);
                trunk.height = height * 0.9f;
                trunk.radius = Mathf.Clamp(height * 0.035f, 0.12f, 0.42f);
            }
            else if (name.StartsWith("rock", StringComparison.Ordinal) || name.StartsWith("stump", StringComparison.Ordinal)
                || name.StartsWith("stone", StringComparison.Ordinal) || name.StartsWith("log", StringComparison.Ordinal))
            {
                Bounds final = ModelBounds(root);
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = final.center;
                collider.size = final.size * 0.85f;
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        public static Bounds ModelBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Mesh has no renderer: " + model.name);
            Bounds result = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }

        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }

        public static Mesh RingMesh()
        {
            const string path = "Assets/Prefabs/FX/MarkerRing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            const int segments = 48;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction;
                vertices[i * 2 + 1] = direction * 0.84f;
                int next = (i + 1) % segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2;
                triangles[t + 3] = next * 2;
                triangles[t + 4] = i * 2 + 1;
                triangles[t + 5] = next * 2 + 1;
            }
            var mesh = new Mesh { name = "Flat marker ring", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
