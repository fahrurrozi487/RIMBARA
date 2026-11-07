using System;
using System.Collections.Generic;

namespace Rimbara.Mission
{
    public readonly struct ProgressSnapshot
    {
        public readonly int Seedlings;
        public readonly int Trees;
        public readonly int Fires;
        public readonly int SeedlingGoal;
        public readonly int TreeGoal;
        public readonly int FireGoal;
        public int CarriedSeedlings => Seedlings - Trees;
        public float Restoration => (float)(Trees + Fires) / (TreeGoal + FireGoal);
        public bool Restored => Seedlings >= SeedlingGoal && Trees >= TreeGoal && Fires >= FireGoal;

        public ProgressSnapshot(int seedlings, int trees, int fires, int seedlingGoal, int treeGoal, int fireGoal)
        {
            Seedlings = seedlings;
            Trees = trees;
            Fires = fires;
            SeedlingGoal = seedlingGoal;
            TreeGoal = treeGoal;
            FireGoal = fireGoal;
        }
    }

    // Pure, deterministic counters. Object IDs make each action a one-time
    // transaction, so holding a key or entering a trigger twice cannot farm progress.
    public sealed class RestorationProgress
    {
        private readonly HashSet<string> seedlings = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> trees = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> fires = new HashSet<string>(StringComparer.Ordinal);
        public int SeedlingGoal { get; }
        public int TreeGoal { get; }
        public int FireGoal { get; }
        public ProgressSnapshot Snapshot => new ProgressSnapshot(seedlings.Count, trees.Count, fires.Count,
            SeedlingGoal, TreeGoal, FireGoal);
        public event Action<ProgressSnapshot> Changed;

        public RestorationProgress(int seedlingGoal, int treeGoal, int fireGoal)
        {
            if (treeGoal < 1 || seedlingGoal < treeGoal || fireGoal < 1)
                throw new ArgumentOutOfRangeException(nameof(seedlingGoal), "Supply enough seedlings for every required tree.");
            SeedlingGoal = seedlingGoal;
            TreeGoal = treeGoal;
            FireGoal = fireGoal;
        }

        public bool TryCollect(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || seedlings.Count >= SeedlingGoal || !seedlings.Add(id)) return false;
            Changed?.Invoke(Snapshot);
            return true;
        }

        public bool TryPlant(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || trees.Count >= TreeGoal || Snapshot.CarriedSeedlings <= 0
                || !trees.Add(id)) return false;
            Changed?.Invoke(Snapshot);
            return true;
        }

        public bool TryExtinguish(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || fires.Count >= FireGoal || !fires.Add(id)) return false;
            Changed?.Invoke(Snapshot);
            return true;
        }

        public void Reset()
        {
            seedlings.Clear();
            trees.Clear();
            fires.Clear();
            Changed?.Invoke(Snapshot);
        }
    }
}
