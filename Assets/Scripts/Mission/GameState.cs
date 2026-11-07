using System;
using UnityEngine;

namespace Rimbara.Mission
{
    [DefaultExecutionOrder(-300)]
    public sealed class GameState : MonoBehaviour
    {
        [SerializeField] private MissionProfile profile;
        public MissionProfile Profile => profile;
        public RestorationProgress Progress { get; private set; }
        public ProgressSnapshot Snapshot => Progress.Snapshot;
        public bool SessionRunning { get; private set; }
        public event Action<ProgressSnapshot> ProgressChanged;
        public event Action<string> SeedlingCollected;
        public event Action<string> TreePlanted;
        public event Action<string> FireExtinguished;

        public void Configure(MissionProfile value) { profile = value; }

        private void Awake()
        {
            if (profile == null || !profile.IsValid)
                throw new InvalidOperationException("Assign a valid RIMBARA MissionProfile to GameState.");
            Progress = profile.CreateProgress();
            Progress.Changed += NotifyProgress;
        }

        public void SetSessionRunning(bool running) { SessionRunning = running; }

        public void ResetSession()
        {
            SessionRunning = false;
            Progress.Reset();
        }

        public bool CollectSeedling(string id)
        {
            if (!SessionRunning || !Progress.TryCollect(id)) return false;
            SeedlingCollected?.Invoke(id);
            return true;
        }

        public bool PlantTree(string id)
        {
            if (!SessionRunning || !Progress.TryPlant(id)) return false;
            TreePlanted?.Invoke(id);
            return true;
        }

        public bool ExtinguishFire(string id)
        {
            if (!SessionRunning || !Progress.TryExtinguish(id)) return false;
            FireExtinguished?.Invoke(id);
            return true;
        }

        private void NotifyProgress(ProgressSnapshot value) { ProgressChanged?.Invoke(value); }

        private void OnDestroy()
        {
            if (Progress != null) Progress.Changed -= NotifyProgress;
        }
    }
}
