using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rimbara.Editor
{
    // Small, original procedural sound assets. No downloaded recording license
    // is implied, and the WAV source files remain editable/LFS-tracked.
    internal static class AudioFactory
    {
        private const int Rate = 22050;

        public static void EnsureClips()
        {
            Ensure("ForestWind", 8f, 31, (t, noise, smooth) => smooth * 3.5f * (0.55f + 0.18f * Mathf.Sin(t * 1.7f)));
            Ensure("ForestBirds", 10f, 42, (t, noise, smooth) =>
            {
                float sample = 0f;
                foreach (float start in new[] { 0.9f, 1.25f, 3.2f, 3.6f, 6.1f, 7.0f, 7.4f })
                {
                    float age = t - start;
                    if (age >= 0f && age < 0.18f)
                        sample += Mathf.Sin(2f * Mathf.PI * (1900f * age + 2400f * age * age))
                            * Mathf.Sin(age / 0.18f * Mathf.PI) * 0.13f;
                }
                return sample;
            });
            Ensure("FireCrackle", 3f, 89, (t, noise, smooth) => noise * (0.09f + 0.12f * Mathf.Pow(Mathf.Sin(t * 37f), 8f)) + smooth * 1.5f);
            Ensure("WaterSpray", 2f, 15, (t, noise, smooth) => noise * 0.12f + smooth * 3f);
            Ensure("CollectSeedling", 0.35f, 2, (t, noise, smooth) => Tone(740f + 160f * t, t) * Envelope(t, 0.35f) * 0.25f);
            Ensure("PlantTree", 0.8f, 2, (t, noise, smooth) =>
                (Tone(261.63f, t) + Tone(392f, t) * 0.6f + Tone(523.25f, t) * 0.35f) * Envelope(t, 0.8f) * 0.2f);
            Ensure("FireOut", 0.9f, 75, (t, noise, smooth) => noise * (1f - t / 0.9f) * 0.18f
                + Tone(587.33f, t) * Envelope(t, 0.9f) * 0.10f);
            Ensure("RestorationComplete", 2.6f, 2, (t, noise, smooth) =>
                (Tone(261.63f, t) + Tone(329.63f, t) * 0.8f + Tone(392f, t) * 0.7f + Tone(523.25f, t) * 0.45f)
                    * Envelope(t, 2.6f) * 0.18f);
        }

        public static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
        private static float Tone(float hz, float t) => Mathf.Sin(t * hz * Mathf.PI * 2f);
        private static float Envelope(float t, float duration) => Mathf.Min(1f, t / 0.025f) * Mathf.Pow(Mathf.Clamp01(1f - t / duration), 1.5f);

        private static void Ensure(string name, float duration, int seed, Func<float, float, float, float> sample)
        {
            string asset = "Assets/Audio/" + name + ".wav";
            string path = Path.Combine(AssetCatalog.Root, asset);
            if (File.Exists(path)) return;
            int count = Mathf.RoundToInt(duration * Rate);
            var random = new System.Random(seed);
            float smooth = 0f;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(Rate);
                writer.Write(Rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)Rate;
                    float noise = (float)random.NextDouble() * 2f - 1f;
                    smooth = Mathf.Lerp(smooth, noise, 0.012f);
                    float edge = Mathf.Min(1f, t / 0.01f, (duration - t) / 0.01f);
                    writer.Write((short)(Mathf.Clamp(sample(t, noise, smooth) * edge, -0.92f, 0.92f) * 32767f));
                }
            }
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(asset);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
    }
}
