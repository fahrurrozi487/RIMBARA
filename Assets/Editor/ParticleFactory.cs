using UnityEditor;
using UnityEngine;

namespace Rimbara.Editor
{
    internal static class ParticleFactory
    {
        public static ParticleSystem Flame(Transform parent)
        {
            ParticleSystem particles = Create("Flames", parent, true, 1.1f, 1.7f, 0.7f,
                new Color(1f, 0.45f, 0.06f, 0.9f), 34f, true);
            particles.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.65f;
            shape.angle = 17f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            colors.color = Fade(new Color(1f, 0.72f, 0.13f), new Color(0.95f, 0.15f, 0.025f), 0.88f);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f),
                new Keyframe(0.25f, 1.15f), new Keyframe(1f, 0.05f)));
            return particles;
        }

        public static ParticleSystem Smoke(Transform parent)
        {
            ParticleSystem particles = Create("Smoke", parent, true, 3.2f, 1.5f, 1.05f,
                new Color(0.22f, 0.23f, 0.19f, 0.33f), 9f, false);
            particles.transform.localPosition = Vector3.up * 0.6f;
            particles.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.5f;
            shape.angle = 14f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            colors.color = Fade(new Color(0.22f, 0.23f, 0.19f), new Color(0.46f, 0.47f, 0.42f), 0.30f);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.45f, 1f, 2.4f));
            return particles;
        }

        public static GameObject Steam(Transform parent)
        {
            var root = new GameObject("Steam after suppression");
            root.transform.SetParent(parent, false);
            ParticleSystem particles = Create("Steam burst", root.transform, false, 1.7f, 1.8f, 0.75f,
                new Color(0.79f, 0.88f, 0.85f, 0.65f), 0f, false);
            particles.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)32) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.75f;
            shape.angle = 28f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(new Color(0.79f, 0.88f, 0.85f), new Color(0.89f, 0.93f, 0.88f), 0.55f);
            root.SetActive(false);
            return root;
        }

        public static GameObject WaterPrefab()
        {
            const string path = "Assets/Prefabs/FX/WaterSpray.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            ParticleSystem particles = Create("Water spray", null, true, 0.40f, 18f, 0.07f,
                new Color(0.40f, 0.80f, 1f, 0.65f), 145f, false);
            var main = particles.main;
            main.playOnAwake = false;
            main.gravityModifier = 0.18f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.025f;
            shape.angle = 3.5f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(new Color(0.41f, 0.80f, 1f), new Color(0.80f, 0.92f, 1f), 0.7f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(particles.gameObject, path);
            Object.DestroyImmediate(particles.gameObject);
            return prefab;
        }

        private static ParticleSystem Create(string name, Transform parent, bool loop, float lifetime,
            float speed, float size, Color color, float rate, bool additive)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.layer = 2;
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 3f;
            main.loop = loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime * 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.75f, speed * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.75f, size * 1.2f);
            main.startColor = color;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetCatalog.ParticleMaterial(additive ? "Fire particles" : "Soft particles", additive);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static Gradient Fade(Color start, Color end, float alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.12f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }
    }
}
