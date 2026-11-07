using System;
using System.Collections.Generic;
using System.Linq;
using Rimbara.Interaction;
using Rimbara.Mission;
using Rimbara.Player;
using Rimbara.UI;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rimbara.Editor
{
    internal static class ProjectVerification
    {
        [Serializable]
        private sealed class Report
        {
            public bool Passed;
            public string Unity;
            public string Utc;
            public string[] Checks;
        }

        public static void Run()
        {
            var checks = new List<string>();
            void Check(string name, bool passed)
            {
                if (!passed) throw new InvalidOperationException("RIMBARA verification failed: " + name);
                checks.Add(name);
            }
            Scene scene = SceneManager.GetActiveScene();
            Check("Main scene is authored and saved", scene.path == ProjectBuilder.ScenePath && !scene.isDirty);
            GameObject[] roots = scene.GetRootGameObjects();
            Transform[] transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            Check("No missing script references", transforms.All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0));
            Check("Three named map zones", new[] { "Zone A - ", "Zone B - ", "Zone C - " }
                .All(prefix => transforms.Count(t => t.name.StartsWith(prefix, StringComparison.Ordinal)) == 1));
            GameState state = transforms.Select(t => t.GetComponent<GameState>()).FirstOrDefault(c => c != null);
            Check("Valid assigned scene profile", state != null && state.Profile != null && state.Profile.IsValid);
            MissionProfile full = AssetDatabase.LoadAssetAtPath<MissionProfile>(ProjectBuilder.FullProfilePath);
            Check("Valid full MVP profile", full != null && full.IsValid
                && full.Seedlings == 5 && full.Trees == 5 && full.Fires == 2);
            MissionProfile week = AssetDatabase.LoadAssetAtPath<MissionProfile>(ProjectBuilder.WeekOneProfilePath);
            Check("Valid Week 1 profile", week != null && week.IsValid && week.Seedlings == 1 && week.Trees == 1 && week.Fires == 1);
            MissionSystem missions = transforms.Select(t => t.GetComponent<MissionSystem>()).FirstOrDefault(c => c != null);
            Check("Mission system owns correct state", missions != null && missions.State == state);
            VolunteerRig rig = transforms.Select(t => t.GetComponent<VolunteerRig>()).FirstOrDefault(c => c != null);
            Check("Official Starter Assets motor", rig != null && rig.OfficialController is ThirdPersonController
                && rig.OfficialController.GetType().Assembly.GetName().Name == "Unity.StarterAssets");
            Check("Volunteer is a prefab instance", PrefabUtility.IsPartOfPrefabInstance(rig.gameObject));
            Check("CharacterController and player tag", rig.GetComponent<CharacterController>() != null && rig.CompareTag("Player"));
            Check("Valid Mixamo Humanoid avatar", rig.Animator != null && rig.Animator.avatar != null && rig.Animator.avatar.isValid && rig.Animator.avatar.isHuman);
            Check("Locomotion animator is present", rig.Animator.runtimeAnimatorController != null
                && rig.Animator.runtimeAnimatorController.animationClips.Length >= 3);
            VolunteerActor actor = rig.GetComponent<VolunteerActor>();
            Check("Actor references missions and state", actor != null && actor.Missions == missions && actor.State == state);
            Check("Interaction detector is configured", HasObject(rig.GetComponent<InteractionDetector>(), "input"));
            WaterSprayer water = rig.GetComponent<WaterSprayer>();
            Check("Water tool camera and nozzle", HasObject(water, "viewCamera") && HasObject(water, "nozzle") && HasObject(water, "spray"));
            SeedlingPickup[] seeds = transforms.Select(t => t.GetComponent<SeedlingPickup>()).Where(c => c != null).ToArray();
            PlantingPoint[] beds = transforms.Select(t => t.GetComponent<PlantingPoint>()).Where(c => c != null).ToArray();
            FirePoint[] fires = transforms.Select(t => t.GetComponent<FirePoint>()).Where(c => c != null).ToArray();
            Check("Five distinct native seedlings", seeds.Length == 5 && seeds.Select(c => c.ObjectId).Distinct().Count() == 5);
            Check("Five distinct planting beds", beds.Length == 5 && beds.Select(c => c.ObjectId).Distinct().Count() == 5);
            Check("Two distinct fire targets", fires.Length == 2 && fires.Select(c => c.ObjectId).Distinct().Count() == 2);
            Check("Seed and bed profile slots", seeds.Select(c => c.Slot).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 5))
                && beds.Select(c => c.Slot).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 5)));
            Check("Fire profile slots", fires.Select(c => c.Slot).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 2)));
            Check("All planting beds have a native tree", beds.All(b => HasObject(b, "treePrefab")));
            Check("All fires have scene references and effects", fires.All(f => HasObject(f, "state") && HasObject(f, "missions")
                && HasObject(f, "glow") && HasObject(f, "steamEffect") && f.GetComponentsInChildren<ParticleSystem>(true).Length >= 3));
            Check("Inspection and post trigger zones", transforms.Count(t => t.GetComponent<MissionZone>() != null) == 2);
            CinemachineCamera gameplay = rig.ViewCamera == null ? null : transforms.Select(t => t.GetComponent<CinemachineCamera>())
                .FirstOrDefault(c => c != null && c.Follow == rig.CameraTarget);
            Check("Cinemachine follows volunteer", gameplay != null && rig.ViewCamera.GetComponent<CinemachineBrain>() != null);
            Check("Third-person camera avoids world obstacles", gameplay.GetComponent<CinemachineThirdPersonFollow>() != null
                && gameplay.GetComponent<CinemachineThirdPersonFollow>().AvoidObstacles.Enabled);
            Check("URP renderer assigned", GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset);
            Check("Paintable terrain with three surface layers", transforms.Select(t => t.GetComponent<Terrain>())
                .Any(t => t != null && t.terrainData != null && t.terrainData.terrainLayers.Length == 3));
            RimbaraHud hud = transforms.Select(t => t.GetComponent<RimbaraHud>()).FirstOrDefault(c => c != null);
            Check("TextMeshPro HUD and restoration bar", hud != null && hud.View != null && hud.View.MissionTitle != null
                && hud.View.Seedlings != null && hud.View.Trees != null && hud.View.Fires != null && hud.View.RestorationFill.sprite != null);
            Check("Generated UI panel and map sprites are opaque", AssetCatalog.HasOpaqueCenter("Assets/UI/RoundedPanel.png")
                && AssetCatalog.HasOpaqueCenter("Assets/UI/Circle.png"));
            Check("Menu, briefing, pause and victory screens", hud.View.Menu != null && hud.View.Briefing != null
                && hud.View.Controls != null && hud.View.Pause != null && hud.View.Victory != null);
            Check("Menu and replay buttons", hud.View.Start != null && hud.View.Begin != null && hud.View.Replay != null && hud.View.Resume != null);
            Check("Main is the only enabled build scene", EditorBuildSettings.scenes.Count(s => s.enabled) == 1
                && EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ProjectBuilder.ScenePath));
            checks.AddRange(ModelVerification.Run());
            ProjectBuilder.WriteArtifact("project-verification.json", JsonUtility.ToJson(new Report
            {
                Passed = true, Unity = Application.unityVersion, Utc = DateTime.UtcNow.ToString("O"), Checks = checks.ToArray()
            }, true));
            Debug.Log("RIMBARA verification passed: " + checks.Count + " structural and deterministic checks.");
        }

        private static bool HasObject(UnityEngine.Object component, string field)
        {
            if (component == null) return false;
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }
    }

    internal static class ModelVerification
    {
        public static string[] Run()
        {
            var checks = new List<string>();
            void Check(string name, bool passed)
            {
                if (!passed) throw new InvalidOperationException("RIMBARA model check failed: " + name);
                checks.Add(name);
            }
            var progress = new RestorationProgress(5, 5, 2);
            int events = 0;
            progress.Changed += snapshot => events++;
            Check("Cannot plant without inventory", !progress.TryPlant("empty") && progress.Snapshot.Trees == 0);
            Check("Blank IDs are rejected", !progress.TryCollect(" ") && !progress.TryPlant(null) && !progress.TryExtinguish(""));
            Check("Collecting commits once", progress.TryCollect("seed-0") && !progress.TryCollect("seed-0") && events == 1);
            Check("Seedlings are not restoration points", progress.Snapshot.Restoration == 0f);
            Check("Planting consumes one carried seedling", progress.TryPlant("bed-0") && progress.Snapshot.CarriedSeedlings == 0);
            Check("Duplicate planting cannot consume inventory", !progress.TryPlant("bed-0") && progress.Snapshot.Trees == 1);
            for (int i = 1; i < 5; i++) { progress.TryCollect("seed-" + i); progress.TryPlant("bed-" + i); }
            Check("Goals cap inventory and planting", !progress.TryCollect("excess") && !progress.TryPlant("excess") && progress.Snapshot.CarriedSeedlings == 0);
            Check("Restoration uses trees plus fires over seven", Math.Abs(progress.Snapshot.Restoration - 5f / 7f) < 0.0001f);
            Check("Fire counter commits once", progress.TryExtinguish("fire-0") && !progress.TryExtinguish("fire-0"));
            Check("Full profile reaches 100 percent", progress.TryExtinguish("fire-1") && progress.Snapshot.Restored && progress.Snapshot.Restoration == 1f);
            Check("Fire goal cannot overflow", !progress.TryExtinguish("excess") && progress.Snapshot.Fires == 2);
            progress.Reset();
            Check("Reset clears progress and inventory", progress.Snapshot.Seedlings == 0 && progress.Snapshot.Trees == 0 && progress.Snapshot.Fires == 0);
            var fire = new FireSuppression(2.4f);
            Check("Invalid water contact is ignored", !fire.ApplyWater(-1f) && !fire.ApplyWater(float.NaN) && !fire.ApplyWater(float.PositiveInfinity) && fire.Fraction == 0f);
            Check("Water contact is cumulative", !fire.ApplyWater(0.8f) && !fire.ApplyWater(0.8f) && fire.Fraction > 0.66f);
            Check("Water completion is one-shot", fire.ApplyWater(1f) && fire.IsOut && fire.Fraction == 1f && !fire.ApplyWater(1f));
            var one = new RestorationProgress(1, 1, 1);
            using (var sequence = new MissionSequence(one))
            {
                int completed = 0;
                sequence.Completed += () => completed++;
                Check("Mission begins with First Steps", sequence.Stage == MissionStage.FirstSteps && !sequence.TryReturn(true));
                Check("Pause blocks inspection", !sequence.TryInspect(false) && sequence.Stage == MissionStage.FirstSteps);
                Check("Inspection starts Seeds of Hope only once", sequence.TryInspect(true) && !sequence.TryInspect(true) && sequence.Stage == MissionStage.SeedsOfHope);
                one.TryCollect("seed");
                Check("Collection alone cannot finish planting mission", sequence.Stage == MissionStage.SeedsOfHope);
                one.TryPlant("bed");
                Check("Planting unlocks Stop the Fire", sequence.Stage == MissionStage.StopTheFire && !sequence.TryReturn(true));
                one.TryExtinguish("fire");
                Check("Restoration requires returning to post", sequence.Stage == MissionStage.ANewBeginning && completed == 0);
                Check("Paused return cannot win", !sequence.TryReturn(false) && completed == 0);
                Check("Post completes mission exactly once", sequence.TryReturn(true) && !sequence.TryReturn(true) && completed == 1 && sequence.Stage == MissionStage.Complete);
                one.Reset();
                sequence.Reset();
                Check("Mission sequence can restart", sequence.Stage == MissionStage.FirstSteps && !one.Snapshot.Restored);
            }
            return checks.ToArray();
        }
    }
}
