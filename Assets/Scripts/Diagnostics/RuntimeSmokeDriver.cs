using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rimbara.Audio;
using Rimbara.Interaction;
using Rimbara.Mission;
using Rimbara.Player;
using Rimbara.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Rimbara.Diagnostics
{
    // Installed only by --rimbara-smoke. Normal games never create this object,
    // virtual devices, fixtures, automatic actions, or diagnostic files.
    public sealed class RuntimeSmokeDriver : MonoBehaviour
    {
        [Serializable]
        private sealed class CheckResult
        {
            public string Name;
            public bool Passed;
            public string Detail;
        }

        [Serializable]
        private sealed class Report
        {
            public bool Passed;
            public string Unity;
            public string Profile;
            public string Utc;
            public string GraphicsDevice;
            public int Width;
            public int Height;
            public float MeanFrameMilliseconds;
            public float P95FrameMilliseconds;
            public CheckResult[] Checks;
            public string[] Captures;
            public string[] Errors;
        }

        private readonly List<CheckResult> checks = new List<CheckResult>();
        private readonly List<string> captures = new List<string>();
        private readonly List<string> errors = new List<string>();
        private readonly List<float> frameTimes = new List<float>();
        private readonly List<MissionStage> stages = new List<MissionStage>();
        private Keyboard keyboard;
        private Mouse mouse;
        private GameState state;
        private MissionSystem missions;
        private GameFlow flow;
        private VolunteerRig rig;
        private VolunteerInput input;
        private VolunteerActor actor;
        private InteractionDetector detector;
        private WaterSprayer water;
        private RimbaraHud hud;
        private WorldGuide guide;
        private GameAudio audioSystem;
        private SeedlingPickup[] seeds;
        private PlantingPoint[] beds;
        private FirePoint[] fires;
        private string output;
        private string profileName;
        private Vector3 spawn;
        private float originalVolume;
        private bool volumeCaptured;
        private bool finished;
        private InputSettings.BackgroundBehavior originalBackgroundBehavior;
        private int collectedEvents;
        private int plantedEvents;
        private int fireEvents;
        private int completedEvents;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (CommandLine.Value("--rimbara-smoke") == null) return;
            var root = new GameObject("RIMBARA opt-in runtime smoke driver");
            DontDestroyOnLoad(root);
            root.AddComponent<RuntimeSmokeDriver>();
        }

        private void Awake()
        {
            Application.logMessageReceived += RecordLog;
            originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            output = Path.GetFullPath(CommandLine.Value("--rimbara-output")
                ?? Path.Combine(Application.persistentDataPath, "Smoke"));
            Directory.CreateDirectory(output);
            keyboard = InputSystem.AddDevice<Keyboard>("RimbaraSmokeKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("RimbaraSmokeMouse");
        }

        // Flatten nested iterators so exceptions in every fixture/action write a
        // failure report instead of silently stopping a Unity coroutine.
        private IEnumerator Start()
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(Run());
            while (stack.Count > 0)
            {
                object current = null;
                bool more = false;
                Exception failure = null;
                try
                {
                    more = stack.Peek().MoveNext();
                    if (more) current = stack.Peek().Current;
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    checks.Add(new CheckResult { Name = "Smoke execution completed", Passed = false, Detail = failure.ToString() });
                    break;
                }
                if (!more) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            Finish();
        }

        private void Update()
        {
            if (!finished && flow != null && flow.Mode == ScreenMode.Playing && Time.unscaledDeltaTime > 0f)
                frameTimes.Add(Time.unscaledDeltaTime * 1000f);
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            FindScene();
            profileName = state.Profile.DisplayName;
            spawn = rig.transform.position;
            originalVolume = audioSystem.Volume;
            volumeCaptured = true;
            stages.Add(missions.Stage);
            missions.MissionChanged += stage => stages.Add(stage);
            missions.Completed += () => completedEvents++;
            state.SeedlingCollected += id => collectedEvents++;
            state.TreePlanted += id => plantedEvents++;
            state.FireExtinguished += id => fireEvents++;

            Check("Menu starts with gameplay disabled", flow.Mode == ScreenMode.Menu && !state.SessionRunning
                && !rig.ControlsEnabled && hud.View.Menu.activeInHierarchy);
            Check("Profile selects the authored object supply", seeds.Count(s => s.gameObject.activeInHierarchy) == state.Profile.Seedlings
                && beds.Count(b => b.gameObject.activeInHierarchy) == state.Profile.Trees
                && fires.Count(f => f.gameObject.activeInHierarchy) == state.Profile.Fires,
                state.Profile.Seedlings + "/" + state.Profile.Trees + "/" + state.Profile.Fires);
            yield return Capture("01-menu.png");
            yield return Click(hud.View.OpenControls);
            Check("Mouse click opens the controls screen", flow.Mode == ScreenMode.Controls && hud.View.Controls.activeInHierarchy);
            yield return Click(hud.View.ControlsBack);
            Check("Mouse click returns from controls to menu", flow.Mode == ScreenMode.Menu);
            hud.View.Volume.value = 0.37f;
            Check("Volume slider is wired", Mathf.Abs(audioSystem.Volume - 0.37f) < 0.001f);
            hud.View.Volume.value = originalVolume;
            yield return Click(hud.View.Start);
            Check("Mouse click opens profile-specific briefing", flow.Mode == ScreenMode.Briefing && hud.View.Briefing.activeInHierarchy
                && hud.View.BriefingTargets.text.Contains(state.Profile.Seedlings + " NATIVE SEEDLINGS"));
            QueueKeys(Key.W, Key.LeftShift);
            yield return new WaitForSecondsRealtime(0.25f);
            Check("Briefing ignores movement input", HorizontalDistance(spawn, rig.transform.position) < 0.04f && !state.SessionRunning);
            QueueKeys();
            yield return Capture("02-briefing.png");
            yield return Click(hud.View.Begin);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("Begin enables the official motor", flow.Mode == ScreenMode.Playing && state.SessionRunning
                && rig.OfficialController.enabled && rig.OfficialController.GetType().Assembly.GetName().Name == "Unity.StarterAssets");
            Check("Vendor input map stays deactivated", !rig.GetComponent<PlayerInput>().inputIsActive);
            Check("Mixamo avatar animates with valid bones", rig.Animator.isHuman && rig.Animator.avatar.isValid
                && rig.Animator.GetBoneTransform(HumanBodyBones.Hips) != null
                && rig.Animator.GetBoneTransform(HumanBodyBones.RightHand) != null);
            Bounds modelBounds = rig.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds;
            Check("Character mesh has human-scale bounds", modelBounds.size.y > 1.2f && modelBounds.size.y < 2.8f,
                "Height " + modelBounds.size.y.ToString("0.000") + " m");
            Check("Idle, walk and run clips are present", rig.Animator.runtimeAnimatorController.animationClips.Length >= 3);

            rig.SetCameraOrbit(0f, 15f);
            yield return new WaitForSecondsRealtime(0.3f);
            Vector3 before = rig.transform.position;
            QueueKeys(Key.W);
            yield return new WaitForSecondsRealtime(0.8f);
            float walkDistance = HorizontalDistance(before, rig.transform.position);
            float walkBlend = rig.Animator.GetFloat("Speed");
            Check("W moves forward through the official motor", walkDistance > 1f && rig.transform.position.z > before.z + 1f,
                walkDistance.ToString("0.000") + " m in 0.8 s");
            Check("Walking drives the locomotion blend", walkBlend > 2f && walkBlend < 3.2f, walkBlend.ToString("0.000"));
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.3f);
            before = rig.transform.position;
            QueueKeys(Key.W, Key.LeftShift);
            yield return new WaitForSecondsRealtime(0.8f);
            float runDistance = HorizontalDistance(before, rig.transform.position);
            Check("Shift running is faster than walking", runDistance > walkDistance * 1.45f,
                "Walk " + walkDistance.ToString("0.000") + " / run " + runDistance.ToString("0.000") + " m");
            Check("Running drives the run animation", rig.Animator.GetFloat("Speed") > 4.4f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.3f);
            QueueKeys(Key.W, Key.D);
            yield return new WaitForSecondsRealtime(0.12f);
            Check("Diagonal input is normalized", input.Move.x > 0.6f && input.Move.y > 0.6f && input.Move.magnitude <= 1.001f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.2f);
            before = rig.transform.position;
            QueueKeys(Key.S);
            yield return new WaitForSecondsRealtime(0.4f);
            Check("S moves backward relative to the camera", rig.transform.position.z < before.z - 0.5f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.2f);
            before = rig.transform.position;
            QueueKeys(Key.A);
            yield return new WaitForSecondsRealtime(0.4f);
            Check("A moves left relative to the camera", rig.transform.position.x < before.x - 0.5f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.2f);
            before = rig.transform.position;
            QueueKeys(Key.D);
            yield return new WaitForSecondsRealtime(0.4f);
            Check("D moves right relative to the camera", rig.transform.position.x > before.x + 0.5f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.3f);
            float yaw = rig.CameraTarget.eulerAngles.y;
            QueueMouse(new Vector2(100f, 0f));
            yield return new WaitForSecondsRealtime(0.25f);
            Check("Mouse delta orbits the third-person camera", Mathf.Abs(Mathf.DeltaAngle(yaw, rig.CameraTarget.eulerAngles.y)) > 6f);
            QueueMouse(new Vector2(0f, 1800f));
            yield return new WaitForSecondsRealtime(0.2f);
            float pitch = Mathf.DeltaAngle(0f, rig.CameraTarget.eulerAngles.x);
            Check("Camera pitch clamps at the official limit", pitch <= rig.OfficialController.TopClamp + rig.OfficialController.CameraAngleOverride + 0.2f
                && pitch > 50f, pitch.ToString("0.0") + " degrees");
            QueueMouse();
            rig.SetCameraOrbit(0f, 15f);

            rig.Teleport(Ground(seeds[0].transform.position + Vector3.back * 1.5f));
            yield return new WaitForSecondsRealtime(0.25f);
            Check("Seedlings are gated by First Steps", !seeds[0].Interact(actor).Success && state.Snapshot.Seedlings == 0);
            Check("Water cannot skip mission order", !fires[0].ReceiveWater(100f) && state.Snapshot.Fires == 0);
            before = rig.transform.position;
            QueueKeys(Key.Escape);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("Escape pauses and freezes time", flow.Mode == ScreenMode.Paused && Time.timeScale == 0f
                && !state.SessionRunning && !rig.ControlsEnabled && rig.Animator.speed == 0f);
            QueueKeys(Key.W, Key.LeftShift, Key.E);
            QueueMouse(default, true);
            yield return new WaitForSecondsRealtime(0.35f);
            Check("Pause blocks motion, interactions and water", HorizontalDistance(before, rig.transform.position) < 0.04f
                && input.Move == Vector2.zero && !water.Spraying && !state.CollectSeedling("paused-fixture")
                && !missions.ReachInspectionMarker() && state.Snapshot.Seedlings == 0);
            QueueKeys();
            QueueMouse();
            RectTransform volumeRect = (RectTransform)hud.View.Volume.transform;
            Vector3 volumePoint = volumeRect.TransformPoint(new Vector3(volumeRect.rect.xMin + volumeRect.rect.width * 0.25f,
                volumeRect.rect.center.y, 0f));
            yield return ClickAt(RectTransformUtility.WorldToScreenPoint(null, volumePoint));
            Check("Mouse pointer changes the pause volume slider", Mathf.Abs(audioSystem.Volume - 0.25f) < 0.06f);
            hud.View.Volume.value = originalVolume;
            yield return Capture("03-pause.png");
            QueueKeys();
            QueueMouse();
            yield return new WaitForSecondsRealtime(0.1f);
            QueueKeys(Key.Escape);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("Escape resumes the session", flow.Mode == ScreenMode.Playing && Time.timeScale == 1f && rig.ControlsEnabled);
            QueueKeys();
            rig.Teleport(new Vector3(70f, -12f, 12f));
            yield return new WaitForSecondsRealtime(0.2f);
            Check("Out-of-bounds recovery preserves progress", HorizontalDistance(spawn, rig.transform.position) < 0.1f
                && rig.transform.position.y > -1f && state.Snapshot.Seedlings == 0);

            yield return MoveTo(missions.ActiveDestination.position);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("Physical inspection trigger starts Seeds of Hope", missions.Stage == MissionStage.SeedsOfHope);
            yield return MoveTo(Ground(beds[0].transform.position + Vector3.back * 1.5f));
            Check("Empty inventory cannot plant", !beds[0].Interact(actor).Success && !beds[0].Planted && state.Snapshot.Trees == 0);
            yield return MoveTo(Ground(seeds[0].transform.position + Vector3.back * 4f));
            detector.Scan();
            Check("Distant seedlings cannot be selected or collected", detector.Focused != seeds[0] && !seeds[0].Interact(actor).Success);
            yield return MoveTo(Ground(seeds[0].transform.position + Vector3.back * 1.5f), 0.2f);
            detector.Scan();
            Check("Nearby detector selects a seedling", detector.Focused == seeds[0]);
            GameObject obstacle = Blocker(Vector3.Lerp(rig.transform.position + Vector3.up * 0.8f,
                seeds[0].FocusPosition, 0.5f), new Vector3(0.6f, 1.1f, 0.3f));
            detector.Scan();
            Check("World obstruction blocks interaction focus", detector.Focused != seeds[0]);
            yield return PressE();
            Check("E cannot collect through a world obstacle", !seeds[0].Collected && state.Snapshot.Seedlings == 0);
            Destroy(obstacle);
            yield return new WaitForSecondsRealtime(0.1f);
            detector.Scan();
            Check("Removing the obstruction restores focus", detector.Focused == seeds[0]);
            yield return Capture("04-seedling.png");
            yield return PressE();
            Check("E collects one seedling and inventory", seeds[0].Collected && state.Snapshot.Seedlings == 1
                && state.Snapshot.CarriedSeedlings == 1 && collectedEvents == 1);
            Check("Duplicate pickup cannot reward twice", !seeds[0].Interact(actor).Success && !state.CollectSeedling(seeds[0].ObjectId)
                && state.Snapshot.CarriedSeedlings == 1);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("Guidance switches to an available planting bed", guide.Destination != null && guide.Destination.GetComponent<PlantingPoint>() != null);
            yield return MoveTo(Ground(beds[0].transform.position + Vector3.back * 1.5f), 0.2f);
            yield return PressE();
            Check("E plants a tree and consumes inventory", beds[0].Planted && beds[0].PlantedTree != null
                && state.Snapshot.Trees == 1 && state.Snapshot.CarriedSeedlings == 0 && plantedEvents == 1);
            Check("Duplicate planting cannot consume another seedling", !beds[0].Interact(actor).Success && !state.PlantTree(beds[0].ObjectId)
                && state.Snapshot.Trees == 1);
            yield return new WaitForSecondsRealtime(1.9f);
            Check("Planted tree grows to its authored size", Mathf.Abs(beds[0].PlantedTree.transform.localScale.y - 1f) < 0.02f);
            yield return Capture("05-new-roots.png");
            for (int i = 1; i < state.Profile.Seedlings; i++)
            {
                yield return MoveTo(Ground(seeds[i].transform.position + Vector3.back * 1.5f), 0.25f);
                yield return PressE();
                Check("Seedling " + (i + 1) + " collected by E", seeds[i].Collected && state.Snapshot.Seedlings == i + 1);
                if (i >= state.Profile.Trees) continue;
                yield return MoveTo(Ground(beds[i].transform.position + Vector3.back * 1.5f), 0.25f);
                yield return PressE();
                Check("Tree " + (i + 1) + " planted by E", beds[i].Planted && state.Snapshot.Trees == i + 1
                    && state.Snapshot.CarriedSeedlings == 0);
            }
            Check("Planting goals unlock Stop the Fire", missions.Stage == MissionStage.StopTheFire
                && state.Snapshot.Seedlings == state.Profile.Seedlings && state.Snapshot.Trees == state.Profile.Trees);
            Check("HUD counters use committed progress", hud.View.Seedlings.text.Contains(state.Profile.Seedlings + " / " + state.Profile.Seedlings)
                && hud.View.Trees.text.Contains(state.Profile.Trees + " / " + state.Profile.Trees)
                && Mathf.Abs(hud.View.RestorationFill.fillAmount - state.Snapshot.Restoration) < 0.001f);

            // Leave the planting clearing sideways before heading north. In the
            // Week 1 profile a straight route from bed 1 would hit its new trunk.
            yield return MoveTo(Ground(new Vector3(3f, 0f, rig.transform.position.z)), 0.35f);
            yield return MoveTo(Ground(fires[0].transform.position + Vector3.back * (state.Profile.SprayRange + 2.2f)), 0.25f);
            Check("Route around planted trees remains traversable", HorizontalDistance(rig.transform.position, fires[0].transform.position)
                < state.Profile.SprayRange + 3f);
            yield return AimAt(fires[0]);
            Check("Camera crosshair selects the distant fire", water.AimedFire == fires[0]);
            yield return SprayFor(0.3f);
            Check("Water range prevents distant suppression", fires[0].SuppressionFraction == 0f && water.AimHint.Contains("Move closer"));
            yield return MoveTo(Ground(fires[0].transform.position + Vector3.back * 4.5f), 0.25f);
            yield return AimAt(fires[0]);
            Check("Near fire is aimable through Cinemachine", water.AimedFire == fires[0]);
            // Put the wall ahead of the volunteer, not behind the shoulder rig:
            // a wall behind the rig correctly makes Cinemachine move the camera
            // forward and would no longer block the resulting camera ray.
            obstacle = Blocker(Vector3.Lerp(rig.transform.position + Vector3.up * 1.5f,
                fires[0].AimPosition, 0.6f), new Vector3(1.2f, 2f, 0.35f));
            yield return SprayFor(0.25f);
            Check("Camera-ray obstruction blocks suppression", water.AimedFire == null && fires[0].SuppressionFraction == 0f,
                "Aim " + (water.AimedFire == null ? "none" : water.AimedFire.ObjectId) + " / contact " + fires[0].SuppressionFraction);
            Destroy(obstacle);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return AimAt(fires[0]);
            rig.transform.rotation = Quaternion.Euler(0f, rig.ViewCamera.transform.eulerAngles.y, 0f);
            water.UpdateAim();
            Transform nozzle = rig.transform.Find("Water nozzle");
            obstacle = Blocker(Vector3.Lerp(nozzle.position, fires[0].AimPosition, 0.24f), Vector3.one * 0.2f);
            water.UpdateAim();
            Check("Nozzle-only obstruction fixture leaves the crosshair clear", water.AimedFire == fires[0]
                && Physics.Linecast(nozzle.position, fires[0].AimPosition, 1 << 8, QueryTriggerInteraction.Ignore));
            Check("Nozzle linecast independently blocks water", !water.ApplySpray(0.5f) && fires[0].SuppressionFraction == 0f
                && water.AimHint.Contains("blocked"));
            Destroy(obstacle);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return AimAt(fires[0]);
            input.EnableTestDriver(false);
            QueueMouse(default, true);
            yield return new WaitForSecondsRealtime(0.6f);
            Check("Left mouse sprays particles and accumulates contact", water.Spraying
                && nozzle.GetComponentInChildren<ParticleSystem>().isPlaying
                && fires[0].SuppressionFraction > 0.05f && fires[0].SuppressionFraction < 0.85f);
            yield return Capture("06-fire-suppression.png");
            QueueMouse();
            yield return new WaitForSecondsRealtime(0.1f);
            float contact = fires[0].SuppressionFraction;
            yield return new WaitForSecondsRealtime(0.4f);
            Check("Releasing water does not erase contact", Mathf.Abs(fires[0].SuppressionFraction - contact) < 0.0001f && !water.Spraying);
            QueueKeys(Key.Escape);
            yield return new WaitForSecondsRealtime(0.1f);
            float elapsed = flow.ElapsedSeconds;
            QueueKeys();
            QueueMouse(default, true);
            yield return new WaitForSecondsRealtime(0.3f);
            Check("Pause preserves fire contact and mission time", flow.Mode == ScreenMode.Paused
                && Mathf.Abs(fires[0].SuppressionFraction - contact) < 0.0001f
                && Mathf.Abs(flow.ElapsedSeconds - elapsed) < 0.0001f && !water.Spraying);
            QueueMouse();
            yield return Click(hud.View.Resume);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("Mouse click resumes gameplay", flow.Mode == ScreenMode.Playing && state.SessionRunning);
            yield return Extinguish(fires[0]);
            Check("Fire completion is one-shot", !fires[0].ReceiveWater(100f) && !state.ExtinguishFire(fires[0].ObjectId)
                && state.Snapshot.Fires == 1 && fireEvents == 1);
            Check("Completed fire stops flames, smoke and collision", fires[0].GetComponentsInChildren<ParticleSystem>(true)
                .Where(p => p.name == "Flames" || p.name == "Smoke").All(p => !p.isPlaying)
                && fires[0].GetComponentsInChildren<Collider>(true).All(c => !c.enabled));
            for (int i = 1; i < state.Profile.Fires; i++)
            {
                yield return MoveTo(Ground(fires[i].transform.position + Vector3.back * 4.5f), 0.25f);
                yield return AimAt(fires[i]);
                yield return Extinguish(fires[i]);
                Check("Fire " + (i + 1) + " extinguished by left mouse", fires[i].Extinguished && state.Snapshot.Fires == i + 1);
            }
            yield return new WaitForSecondsRealtime(0.2f);
            Check("100 percent requires returning to the post", state.Snapshot.Restored && state.Snapshot.Restoration == 1f
                && missions.Stage == MissionStage.ANewBeginning && flow.Mode == ScreenMode.Playing && completedEvents == 0);
            Check("HUD and guidance reflect full restoration", hud.View.Restoration.text == "100% RESTORED"
                && hud.View.RestorationFill.fillAmount == 1f && guide.Destination == missions.ActiveDestination);
            yield return Capture("07-restored-forest.png");
            Vector3 post = missions.ActiveDestination.position;
            yield return MoveTo(Ground(new Vector3(3f, 0f, 35f)), 0.5f);
            yield return MoveTo(Ground(new Vector3(3f, 0f, -18f)), 0.5f);
            yield return MoveTo(post, 1.2f, true);
            yield return new WaitForSecondsRealtime(0.3f);
            Check("Physical post trigger wins exactly once", missions.Stage == MissionStage.Complete && completedEvents == 1
                && flow.Mode == ScreenMode.Victory && !state.SessionRunning);
            Check("Victory screen releases controls and cursor", hud.View.Victory.activeInHierarchy && !hud.View.Gameplay.activeInHierarchy
                && !rig.ControlsEnabled && Cursor.lockState == CursorLockMode.None && !water.Spraying);
            Check("Action events match profile goals", collectedEvents == state.Profile.Seedlings
                && plantedEvents == state.Profile.Trees && fireEvents == state.Profile.Fires);
            Check("All four missions execute in order", stages.SequenceEqual(new[] { MissionStage.FirstSteps, MissionStage.SeedsOfHope,
                MissionStage.StopTheFire, MissionStage.ANewBeginning, MissionStage.Complete }), string.Join(" -> ", stages));
            yield return Capture("08-victory.png");
            yield return Click(hud.View.Replay);
            yield return new WaitForSecondsRealtime(0.7f);
            FindScene();
            Check("Replay reloads a clean briefing", flow.Mode == ScreenMode.Briefing && !state.SessionRunning
                && state.Snapshot.Seedlings == 0 && state.Snapshot.Trees == 0 && state.Snapshot.Fires == 0);
            Check("Replay restores visual objects and the profile", seeds.Where(s => s.gameObject.activeInHierarchy).All(s => !s.Collected)
                && beds.Where(b => b.gameObject.activeInHierarchy).All(b => !b.Planted)
                && fires.Where(f => f.gameObject.activeInHierarchy).All(f => !f.Extinguished && f.SuppressionFraction == 0f)
                && state.Profile.DisplayName == profileName);
            yield return Click(hud.View.Begin);
            yield return new WaitForSecondsRealtime(0.15f);
            Check("Replay can begin First Steps again", flow.Mode == ScreenMode.Playing && missions.Stage == MissionStage.FirstSteps);
            flow.Pause();
            yield return Click(hud.View.PauseMenu);
            yield return new WaitForSecondsRealtime(0.6f);
            FindScene();
            Check("Pause-menu return resets the session", flow.Mode == ScreenMode.Menu && hud.View.Menu.activeInHierarchy
                && state.Snapshot.Seedlings == 0 && missions.Stage == MissionStage.FirstSteps && Time.timeScale == 1f);
            Check("Runtime produced no error or exception logs", errors.Count == 0, string.Join("\n", errors));
        }

        private void FindScene()
        {
            state = FindFirstObjectByType<GameState>();
            missions = FindFirstObjectByType<MissionSystem>();
            flow = FindFirstObjectByType<GameFlow>();
            rig = FindFirstObjectByType<VolunteerRig>();
            hud = FindFirstObjectByType<RimbaraHud>();
            guide = FindFirstObjectByType<WorldGuide>();
            audioSystem = FindFirstObjectByType<GameAudio>();
            if (state == null || missions == null || flow == null || rig == null || hud == null || guide == null || audioSystem == null)
                throw new InvalidOperationException("The authored Main scene is missing runtime systems.");
            input = rig.GetComponent<VolunteerInput>();
            actor = rig.GetComponent<VolunteerActor>();
            detector = rig.GetComponent<InteractionDetector>();
            water = rig.GetComponent<WaterSprayer>();
            seeds = FindObjectsByType<SeedlingPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(s => s.Slot).ToArray();
            beds = FindObjectsByType<PlantingPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(b => b.Slot).ToArray();
            fires = FindObjectsByType<FirePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(f => f.Slot).ToArray();
        }

        private IEnumerator MoveTo(Vector3 destination, float radius = 0.5f, bool stopAtVictory = false)
        {
            input.EnableTestDriver(true);
            ClearDriver();
            float deadline = Time.realtimeSinceStartup + 35f;
            while (HorizontalDistance(rig.transform.position, destination) > radius)
            {
                if (stopAtVictory && flow.Mode == ScreenMode.Victory) break;
                if (!state.SessionRunning || Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Movement route could not reach " + destination + " from " + rig.transform.position);
                Vector3 difference = destination - rig.transform.position;
                difference.y = 0f;
                Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, rig.ViewCamera.transform.eulerAngles.y, 0f)) * difference.normalized;
                bool run = difference.magnitude > 2f;
                input.SetTestInput(new Vector2(local.x, local.z), Vector2.zero, run, false, false);
                yield return null;
            }
            ClearDriver();
            yield return new WaitForSecondsRealtime(0.18f);
        }

        private IEnumerator PressE()
        {
            ClearDriver();
            input.EnableTestDriver(false);
            QueueKeys(Key.E);
            yield return new WaitForSecondsRealtime(0.12f);
            QueueKeys();
            yield return new WaitForSecondsRealtime(0.12f);
            input.EnableTestDriver(true);
        }

        private IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform rect = (RectTransform)button.transform;
            Vector3 center = rect.TransformPoint(rect.rect.center);
            yield return ClickAt(RectTransformUtility.WorldToScreenPoint(null, center));
        }

        private IEnumerator ClickAt(Vector2 position)
        {
            input.EnableTestDriver(false);
            QueueMouse(position: position);
            yield return new WaitForSecondsRealtime(0.08f);
            QueueMouse(left: true, position: position);
            yield return new WaitForSecondsRealtime(0.1f);
            QueueMouse(position: position);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        private IEnumerator AimAt(FirePoint target)
        {
            input.EnableTestDriver(true);
            ClearDriver();
            for (int i = 0; i < 7; i++)
            {
                Quaternion rotation = Quaternion.LookRotation(target.AimPosition - rig.ViewCamera.transform.position);
                float pitch = Mathf.DeltaAngle(0f, rotation.eulerAngles.x) - rig.OfficialController.CameraAngleOverride;
                rig.SetCameraOrbit(rotation.eulerAngles.y, pitch);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            water.UpdateAim();
        }

        private IEnumerator SprayFor(float seconds)
        {
            ClearDriver();
            input.EnableTestDriver(false);
            QueueMouse(default, true);
            yield return new WaitForSecondsRealtime(seconds);
            QueueMouse();
            yield return new WaitForSecondsRealtime(0.1f);
            input.EnableTestDriver(true);
        }

        private IEnumerator Extinguish(FirePoint target)
        {
            input.EnableTestDriver(false);
            QueueKeys();
            QueueMouse(default, true);
            float deadline = Time.realtimeSinceStartup + state.Profile.ExtinguishSeconds + 5f;
            while (!target.Extinguished && Time.realtimeSinceStartup < deadline) yield return null;
            QueueMouse();
            yield return new WaitForSecondsRealtime(0.15f);
            Check("Water contact completes " + target.ObjectId, target.Extinguished, "Contact " + target.SuppressionFraction.ToString("0.000"));
            input.EnableTestDriver(true);
        }

        private IEnumerator Capture(string filename)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(output, filename), image.EncodeToPNG()); }
            finally { Destroy(image); }
            captures.Add(filename);
        }

        private static Vector3 Ground(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y + 0.06f;
            return position;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static GameObject Blocker(Vector3 center, Vector3 size)
        {
            var root = new GameObject("Smoke-only obstruction fixture");
            root.layer = 8;
            root.transform.position = center;
            root.AddComponent<BoxCollider>().size = size;
            Physics.SyncTransforms();
            return root;
        }

        private void ClearDriver() { input.SetTestInput(Vector2.zero, Vector2.zero, false, false, false); }
        private void QueueKeys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        private void QueueMouse(Vector2 delta = default, bool left = false, Vector2? position = null)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position ?? new Vector2(40f, 40f), delta = delta }.WithButton(MouseButton.Left, left));
        }

        private void Check(string name, bool passed, string detail = "")
        {
            checks.Add(new CheckResult { Name = name, Passed = passed, Detail = detail });
            if (!passed) throw new InvalidOperationException("Runtime smoke check failed: " + name + " / " + detail);
        }

        private void RecordLog(string condition, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(condition + "\n" + trace);
        }

        private void Finish()
        {
            finished = true;
            if (volumeCaptured && audioSystem != null) audioSystem.SetVolume(originalVolume);
            Application.logMessageReceived -= RecordLog;
            Time.timeScale = 1f;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = originalBackgroundBehavior;
            float[] sorted = frameTimes.OrderBy(t => t).ToArray();
            bool passed = checks.Count > 0 && checks.All(c => c.Passed) && errors.Count == 0;
            var report = new Report
            {
                Passed = passed, Unity = Application.unityVersion, Profile = profileName, Utc = DateTime.UtcNow.ToString("O"),
                GraphicsDevice = SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType,
                Width = Screen.width, Height = Screen.height,
                MeanFrameMilliseconds = sorted.Length == 0 ? 0f : sorted.Average(),
                P95FrameMilliseconds = sorted.Length == 0 ? 0f : sorted[Mathf.Min(sorted.Length - 1, Mathf.FloorToInt(sorted.Length * 0.95f))],
                Checks = checks.ToArray(), Captures = captures.ToArray(), Errors = errors.ToArray()
            };
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("RIMBARA runtime smoke " + (passed ? "PASSED" : "FAILED") + ": " + checks.Count + " checks; " + output);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(passed ? 0 : 1);
#endif
        }
    }
}
