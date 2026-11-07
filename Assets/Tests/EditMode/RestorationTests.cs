using System;
using NUnit.Framework;
using Rimbara.Interaction;
using Rimbara.Mission;
using UnityEngine;

namespace Rimbara.Tests
{
    public sealed class RestorationTests
    {
        [TestCase(1, 1, 1)]
        [TestCase(5, 5, 2)]
        public void BothProfilesCompleteWithoutInventoryLoss(int seedlings, int trees, int fires)
        {
            var model = new RestorationProgress(seedlings, trees, fires);
            for (int i = 0; i < seedlings; i++) Assert.That(model.TryCollect("seed-" + i), Is.True);
            Assert.That(model.Snapshot.CarriedSeedlings, Is.EqualTo(seedlings));
            for (int i = 0; i < trees; i++) Assert.That(model.TryPlant("bed-" + i), Is.True);
            for (int i = 0; i < fires; i++) Assert.That(model.TryExtinguish("fire-" + i), Is.True);
            Assert.That(model.Snapshot.Restored, Is.True);
            Assert.That(model.Snapshot.Restoration, Is.EqualTo(1f));
            Assert.That(model.Snapshot.CarriedSeedlings, Is.EqualTo(seedlings - trees));
        }

        [TestCase(0, 1, 1)]
        [TestCase(1, 2, 1)]
        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        public void InvalidGoalCombinationsAreRejected(int seeds, int trees, int fires)
        {
            Assert.That(() => new RestorationProgress(seeds, trees, fires), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EmptyInventoryCannotPlant()
        {
            var model = new RestorationProgress(5, 5, 2);
            Assert.That(model.TryPlant("bed"), Is.False);
            Assert.That(model.Snapshot.Trees, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void InvalidObjectIdsNeverEarnProgress(string id)
        {
            var model = new RestorationProgress(1, 1, 1);
            Assert.That(model.TryCollect(id), Is.False);
            Assert.That(model.TryExtinguish(id), Is.False);
            model.TryCollect("valid");
            Assert.That(model.TryPlant(id), Is.False);
        }

        [Test]
        public void DuplicateActionsEmitNoAdditionalEvents()
        {
            var model = new RestorationProgress(2, 2, 1);
            int count = 0;
            model.Changed += progress => count++;
            Assert.That(model.TryCollect("one"), Is.True);
            Assert.That(model.TryCollect("one"), Is.False);
            Assert.That(model.TryPlant("bed"), Is.True);
            model.TryCollect("two");
            Assert.That(model.TryPlant("bed"), Is.False);
            Assert.That(model.Snapshot.CarriedSeedlings, Is.EqualTo(1));
            Assert.That(model.TryExtinguish("fire"), Is.True);
            Assert.That(model.TryExtinguish("fire"), Is.False);
            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void SeedlingsDoNotDoubleCountRestoration()
        {
            var model = new RestorationProgress(5, 5, 2);
            for (int i = 0; i < 5; i++) model.TryCollect("seed" + i);
            Assert.That(model.Snapshot.Restoration, Is.Zero);
            model.TryPlant("bed");
            Assert.That(model.Snapshot.Restoration, Is.EqualTo(1f / 7f).Within(0.00001f));
            model.TryExtinguish("fire");
            Assert.That(model.Snapshot.Restoration, Is.EqualTo(2f / 7f).Within(0.00001f));
        }

        [Test]
        public void GoalsCannotOverflow()
        {
            var model = new RestorationProgress(1, 1, 1);
            model.TryCollect("seed"); model.TryPlant("bed"); model.TryExtinguish("fire");
            Assert.That(model.TryCollect("extra"), Is.False);
            Assert.That(model.TryPlant("extra"), Is.False);
            Assert.That(model.TryExtinguish("extra"), Is.False);
            Assert.That(model.Snapshot.Restoration, Is.EqualTo(1f));
        }

        [Test]
        public void ResetMakesObjectIdsAvailableAgain()
        {
            var model = new RestorationProgress(1, 1, 1);
            model.TryCollect("seed"); model.TryPlant("bed"); model.TryExtinguish("fire");
            model.Reset();
            Assert.That(model.Snapshot.Restored, Is.False);
            Assert.That(model.TryCollect("seed"), Is.True);
            Assert.That(model.TryPlant("bed"), Is.True);
            Assert.That(model.TryExtinguish("fire"), Is.True);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidSuppressionDurationIsRejected(float seconds)
        {
            Assert.That(() => new FireSuppression(seconds), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0f)]
        [TestCase(-2f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidWaterContactDoesNotAdvanceFire(float dt)
        {
            var fire = new FireSuppression(2.4f);
            Assert.That(fire.ApplyWater(dt), Is.False);
            Assert.That(fire.ContactSeconds, Is.Zero);
        }

        [Test]
        public void IntermittentWaterNeverLosesProgress()
        {
            var fire = new FireSuppression(2.4f);
            fire.ApplyWater(0.8f);
            float before = fire.Fraction;
            fire.ApplyWater(0f);
            Assert.That(fire.Fraction, Is.EqualTo(before));
            fire.ApplyWater(0.8f);
            Assert.That(fire.ApplyWater(1f), Is.True);
            Assert.That(fire.Fraction, Is.EqualTo(1f));
            Assert.That(fire.ApplyWater(100f), Is.False);
        }

        [Test]
        public void MissionsAdvanceInTheRequiredOrderAndWinAtPost()
        {
            var model = new RestorationProgress(1, 1, 1);
            using (var sequence = new MissionSequence(model))
            {
                int completed = 0;
                sequence.Completed += () => completed++;
                Assert.That(sequence.TryReturn(true), Is.False);
                Assert.That(sequence.TryInspect(true), Is.True);
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.SeedsOfHope));
                Assert.That(sequence.TryInspect(true), Is.False);
                model.TryCollect("seed");
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.SeedsOfHope));
                model.TryPlant("bed");
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.StopTheFire));
                model.TryExtinguish("fire");
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.ANewBeginning));
                Assert.That(completed, Is.Zero);
                Assert.That(sequence.TryReturn(true), Is.True);
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.Complete));
                Assert.That(sequence.TryReturn(true), Is.False);
                Assert.That(completed, Is.EqualTo(1));
            }
        }

        [Test]
        public void PauseBlocksInspectionAndVictory()
        {
            var model = new RestorationProgress(1, 1, 1);
            using (var sequence = new MissionSequence(model))
            {
                Assert.That(sequence.TryInspect(false), Is.False);
                sequence.TryInspect(true);
                model.TryCollect("seed"); model.TryPlant("bed"); model.TryExtinguish("fire");
                Assert.That(sequence.TryReturn(false), Is.False);
                Assert.That(sequence.Stage, Is.EqualTo(MissionStage.ANewBeginning));
            }
        }

        [Test]
        public void DisposedSequenceStopsListeningToProgress()
        {
            var model = new RestorationProgress(1, 1, 1);
            var sequence = new MissionSequence(model);
            sequence.TryInspect(true);
            sequence.Dispose();
            model.TryCollect("seed"); model.TryPlant("bed");
            Assert.That(sequence.Stage, Is.EqualTo(MissionStage.SeedsOfHope));
        }

        [TestCase(1, 1, 1, true)]
        [TestCase(5, 5, 2, true)]
        [TestCase(5, 4, 2, true)]
        [TestCase(6, 5, 2, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(5, 5, 3, false)]
        public void ProfilesRespectTheAuthoredMapSupply(int seeds, int trees, int fires, bool valid)
        {
            var profile = ScriptableObject.CreateInstance<MissionProfile>();
            try
            {
                profile.Seedlings = seeds; profile.Trees = trees; profile.Fires = fires;
                Assert.That(profile.IsValid, Is.EqualTo(valid));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void ProfilesRejectNonFiniteOrNonPositiveToolSettings(float value)
        {
            var profile = ScriptableObject.CreateInstance<MissionProfile>();
            try
            {
                profile.ExtinguishSeconds = value;
                Assert.That(profile.IsValid, Is.False);
                profile.ExtinguishSeconds = 2.4f;
                profile.SprayRange = value;
                Assert.That(profile.IsValid, Is.False);
                profile.SprayRange = 7f;
                profile.InteractionRange = value;
                Assert.That(profile.IsValid, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
    }
}
