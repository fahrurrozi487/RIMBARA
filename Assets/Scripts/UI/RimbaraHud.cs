using Rimbara.Audio;
using Rimbara.Interaction;
using Rimbara.Mission;
using Rimbara.Player;
using UnityEngine;

namespace Rimbara.UI
{
    [DefaultExecutionOrder(200)]
    public sealed class RimbaraHud : MonoBehaviour
    {
        [SerializeField] private HudReferences view;
        [SerializeField] private GameState state;
        [SerializeField] private MissionSystem missions;
        [SerializeField] private GameFlow flow;
        [SerializeField] private VolunteerActor actor;
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private WaterSprayer sprayer;
        [SerializeField] private WorldGuide guide;
        [SerializeField] private VolunteerRig rig;
        [SerializeField] private GameAudio audioSystem;
        private float toastUntil;
        public HudReferences View => view;

        public void Configure(HudReferences references, GameState gameState, MissionSystem missionSystem,
            GameFlow gameFlow, VolunteerActor volunteer, InteractionDetector interaction, WaterSprayer water,
            WorldGuide worldGuide, VolunteerRig player, GameAudio sounds)
        {
            view = references;
            state = gameState;
            missions = missionSystem;
            flow = gameFlow;
            actor = volunteer;
            detector = interaction;
            sprayer = water;
            guide = worldGuide;
            rig = player;
            audioSystem = sounds;
        }

        private void Awake()
        {
            state.ProgressChanged += RefreshProgress;
            missions.MissionChanged += RefreshMission;
            flow.ModeChanged += RefreshMode;
            actor.ActionPerformed += ShowAction;
            view.Start.onClick.AddListener(flow.ShowBriefing);
            view.Begin.onClick.AddListener(flow.Begin);
            view.OpenControls.onClick.AddListener(flow.ShowControls);
            view.ControlsBack.onClick.AddListener(flow.ShowMenu);
            view.BriefingBack.onClick.AddListener(flow.ShowMenu);
            view.Resume.onClick.AddListener(flow.Resume);
            view.PauseMenu.onClick.AddListener(flow.ReturnToMenu);
            view.Replay.onClick.AddListener(flow.Replay);
            view.VictoryMenu.onClick.AddListener(flow.ReturnToMenu);
            foreach (var button in view.Quit) button.onClick.AddListener(flow.Quit);
            view.Volume.SetValueWithoutNotify(audioSystem.Volume);
            view.Volume.onValueChanged.AddListener(audioSystem.SetVolume);
        }

        private void Start()
        {
            RefreshProgress(state.Snapshot);
            RefreshMission(missions.Stage);
            RefreshMode(flow.Mode);
        }

        private void Update()
        {
            if (flow.Mode != ScreenMode.Playing && flow.Mode != ScreenMode.Paused) return;
            string prompt = detector.Prompt;
            if (string.IsNullOrEmpty(prompt)) prompt = sprayer.AimHint;
            view.Interaction.text = prompt;
            view.Interaction.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            if (guide.Destination != null)
            {
                Vector3 toTarget = guide.Destination.position - actor.transform.position;
                toTarget.y = 0f;
                Vector3 forward = rig.ViewCamera.transform.forward;
                forward.y = 0f;
                float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);
                string direction = angle > 35f ? "RIGHT" : angle < -35f ? "LEFT" : "AHEAD";
                if (Mathf.Abs(angle) > 140f) direction = "BEHIND";
                view.Waypoint.text = guide.DestinationName.ToUpperInvariant() + "   " + guide.Distance.ToString("0") + " m  /  " + direction;
            }
            view.PlayerDot.anchoredPosition = MapPosition(actor.transform.position);
            view.TargetDot.gameObject.SetActive(guide.Destination != null);
            if (guide.Destination != null) view.TargetDot.anchoredPosition = MapPosition(guide.Destination.position);
            bool aimed = sprayer.AimedFire != null && missions.Stage == MissionStage.StopTheFire && !sprayer.AimedFire.Extinguished;
            view.FireProgress.SetActive(aimed);
            if (aimed) view.FireFill.fillAmount = sprayer.AimedFire.SuppressionFraction;
            view.Toast.transform.parent.gameObject.SetActive(Time.unscaledTime < toastUntil);
        }

        private static Vector2 MapPosition(Vector3 world)
        {
            return new Vector2(Mathf.Clamp(world.x / 60f, -1f, 1f) * 77f,
                Mathf.Clamp((world.z - 22f) / 65f, -1f, 1f) * 82f);
        }

        private void RefreshProgress(ProgressSnapshot progress)
        {
            view.Seedlings.text = "SEEDLINGS   <b>" + progress.Seedlings + " / " + progress.SeedlingGoal + "</b>"
                + "  <color=#95BDA3>(carrying " + progress.CarriedSeedlings + ")</color>";
            view.Trees.text = "TREES PLANTED   <b>" + progress.Trees + " / " + progress.TreeGoal + "</b>";
            view.Fires.text = "FIRES OUT   <b>" + progress.Fires + " / " + progress.FireGoal + "</b>";
            view.Restoration.text = Mathf.RoundToInt(progress.Restoration * 100f) + "% RESTORED";
            view.RestorationFill.fillAmount = progress.Restoration;
            view.BriefingTargets.text = progress.SeedlingGoal + " NATIVE SEEDLINGS   /   " + progress.TreeGoal + " TREES   /   " + progress.FireGoal + " FIRES";
        }

        private void RefreshMission(MissionStage stage)
        {
            int index = Mathf.Min(4, (int)stage + 1);
            view.MissionNumber.text = "FIELD ASSIGNMENT    " + index.ToString("00") + " / 04";
            view.MissionTitle.text = missions.Title;
            view.MissionDescription.text = missions.Instruction;
            if (flow.Mode == ScreenMode.Playing)
                ShowToast("MISSION " + index + "  /  " + missions.Title, true);
        }

        private void RefreshMode(ScreenMode mode)
        {
            view.Gameplay.SetActive(mode == ScreenMode.Playing || mode == ScreenMode.Paused);
            view.Menu.SetActive(mode == ScreenMode.Menu);
            view.Briefing.SetActive(mode == ScreenMode.Briefing);
            view.Controls.SetActive(mode == ScreenMode.Controls);
            view.Pause.SetActive(mode == ScreenMode.Paused);
            view.Victory.SetActive(mode == ScreenMode.Victory);
            view.Dimmer.SetActive(mode != ScreenMode.Playing);
            if (mode == ScreenMode.Victory)
            {
                ProgressSnapshot progress = state.Snapshot;
                int minutes = Mathf.FloorToInt(flow.ElapsedSeconds / 60f);
                int seconds = Mathf.FloorToInt(flow.ElapsedSeconds % 60f);
                view.VictoryResult.text = "<b>" + progress.Trees + " trees planted   /   " + progress.Fires + " fires extinguished</b>\n"
                    + "100% of your restoration mission complete\n\nField time  " + minutes.ToString("00") + ":" + seconds.ToString("00");
            }
        }

        private void ShowAction(InteractionResult result) { ShowToast(result.Message, result.Success); }

        public void ShowToast(string text, bool success = true)
        {
            view.Toast.text = text;
            view.Toast.color = success ? new Color(0.85f, 0.97f, 0.87f) : new Color(1f, 0.8f, 0.52f);
            toastUntil = Time.unscaledTime + 5.5f;
            view.Toast.transform.parent.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (state != null) state.ProgressChanged -= RefreshProgress;
            if (missions != null) missions.MissionChanged -= RefreshMission;
            if (flow != null) flow.ModeChanged -= RefreshMode;
            if (actor != null) actor.ActionPerformed -= ShowAction;
        }
    }
}
