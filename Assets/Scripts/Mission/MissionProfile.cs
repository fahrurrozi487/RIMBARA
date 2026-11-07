using UnityEngine;

namespace Rimbara.Mission
{
    [CreateAssetMenu(menuName = "RIMBARA/Mission profile", fileName = "ArunikaMVP")]
    public sealed class MissionProfile : ScriptableObject
    {
        [Min(1)] public int Seedlings = 5;
        [Min(1)] public int Trees = 5;
        [Min(1)] public int Fires = 2;
        [Min(0.2f)] public float ExtinguishSeconds = 2.4f;
        [Min(1f)] public float SprayRange = 7f;
        [Min(0.5f)] public float InteractionRange = 2.5f;
        public string DisplayName = "Arunika restoration";

        public bool IsValid => Seedlings >= Trees && Trees > 0 && Fires > 0
            && Seedlings <= 5 && Trees <= 5 && Fires <= 2
            && PositiveFinite(ExtinguishSeconds) && PositiveFinite(SprayRange) && PositiveFinite(InteractionRange);

        private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);

        public RestorationProgress CreateProgress()
        {
            if (!IsValid) throw new System.InvalidOperationException("Mission targets exceed the available map objects.");
            return new RestorationProgress(Seedlings, Trees, Fires);
        }
    }
}
