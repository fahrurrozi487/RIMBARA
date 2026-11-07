using System;
using UnityEngine;

namespace Rimbara.Mission
{
    public enum MissionStage { FirstSteps, SeedsOfHope, StopTheFire, ANewBeginning, Complete }

    // Ordered state machine; counters are owned by GameState, not by the HUD.
    [DefaultExecutionOrder(-250)]
    public sealed class MissionSystem : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private Transform inspectionMarker;
        [SerializeField] private Transform volunteerPost;
        [SerializeField] private Transform plantingZone;
        [SerializeField] private Transform fireZone;
        private MissionSequence sequence;
        public MissionStage Stage => sequence == null ? MissionStage.FirstSteps : sequence.Stage;
        public GameState State => state;
        public Transform ActiveDestination => Stage == MissionStage.FirstSteps ? inspectionMarker
            : Stage == MissionStage.SeedsOfHope ? plantingZone
            : Stage == MissionStage.StopTheFire ? fireZone : volunteerPost;
        public event Action<MissionStage> MissionChanged;
        public event Action Completed;
        public bool CanCollectOrPlant => state.SessionRunning && Stage == MissionStage.SeedsOfHope;
        public bool CanExtinguish => state.SessionRunning && Stage == MissionStage.StopTheFire;

        public void Configure(GameState gameState, Transform inspection, Transform post, Transform planting, Transform fire)
        {
            state = gameState;
            inspectionMarker = inspection;
            volunteerPost = post;
            plantingZone = planting;
            fireZone = fire;
        }

        private void Awake()
        {
            if (state == null) throw new InvalidOperationException("MissionSystem needs a GameState.");
            sequence = new MissionSequence(state.Progress);
            sequence.Changed += OnMissionChanged;
            sequence.Completed += OnCompleted;
        }

        public void ResetMissions()
        {
            sequence.Reset();
        }

        public bool ReachInspectionMarker()
        {
            return sequence.TryInspect(state.SessionRunning);
        }

        public bool ReturnToPost()
        {
            return sequence.TryReturn(state.SessionRunning);
        }

        private void OnMissionChanged(MissionStage stage) { MissionChanged?.Invoke(stage); }
        private void OnCompleted() { Completed?.Invoke(); }

        public string Title => Stage == MissionStage.FirstSteps ? "FIRST STEPS"
            : Stage == MissionStage.SeedsOfHope ? "SEEDS OF HOPE"
            : Stage == MissionStage.StopTheFire ? "STOP THE FIRE"
            : Stage == MissionStage.ANewBeginning ? "A NEW BEGINNING" : "ARUNIKA RESTORED";

        public string Instruction
        {
            get
            {
                ProgressSnapshot progress = state.Snapshot;
                switch (Stage)
                {
                    case MissionStage.FirstSteps: return "Walk to the amber marker and inspect the damaged forest.";
                    case MissionStage.SeedsOfHope:
                        return "Collect " + progress.SeedlingGoal + " native seedlings and plant " + progress.TreeGoal + " trees. Press E near an object.";
                    case MissionStage.StopTheFire:
                        return "Approach the " + progress.FireGoal + " remaining fires. Aim the crosshair and hold the left mouse button to spray.";
                    case MissionStage.ANewBeginning: return "Every fire is out. Return to the volunteer post to finish your work.";
                    default: return "One act of care became a new beginning for Arunika.";
                }
            }
        }

        private void OnDestroy()
        {
            if (sequence == null) return;
            sequence.Changed -= OnMissionChanged;
            sequence.Completed -= OnCompleted;
            sequence.Dispose();
        }
    }
}
