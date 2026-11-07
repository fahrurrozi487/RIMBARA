using System;

namespace Rimbara.Mission
{
    // Engine-independent ordered mission logic. The Unity component supplies
    // session/pause state, and physical interactions remain in their own scripts.
    public sealed class MissionSequence : IDisposable
    {
        private readonly RestorationProgress progress;
        public MissionStage Stage { get; private set; } = MissionStage.FirstSteps;
        public event Action<MissionStage> Changed;
        public event Action Completed;

        public MissionSequence(RestorationProgress model)
        {
            progress = model ?? throw new ArgumentNullException(nameof(model));
            progress.Changed += OnProgress;
        }

        public bool TryInspect(bool sessionRunning)
        {
            if (!sessionRunning || Stage != MissionStage.FirstSteps) return false;
            Advance(MissionStage.SeedsOfHope);
            OnProgress(progress.Snapshot);
            return true;
        }

        public bool TryReturn(bool sessionRunning)
        {
            if (!sessionRunning || Stage != MissionStage.ANewBeginning || !progress.Snapshot.Restored) return false;
            Advance(MissionStage.Complete);
            Completed?.Invoke();
            return true;
        }

        private void OnProgress(ProgressSnapshot snapshot)
        {
            if (Stage == MissionStage.SeedsOfHope && snapshot.Trees >= snapshot.TreeGoal && snapshot.Seedlings >= snapshot.SeedlingGoal)
                Advance(MissionStage.StopTheFire);
            if (Stage == MissionStage.StopTheFire && snapshot.Fires >= snapshot.FireGoal)
                Advance(MissionStage.ANewBeginning);
        }

        private void Advance(MissionStage next) { Stage = next; Changed?.Invoke(next); }
        public void Reset() { Stage = MissionStage.FirstSteps; Changed?.Invoke(Stage); }
        public void Dispose() { progress.Changed -= OnProgress; }
    }
}
