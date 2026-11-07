using System;

namespace Rimbara.Interaction
{
    // Water contact accumulates; there is no damage, ammunition limit, or fail state.
    public sealed class FireSuppression
    {
        public float RequiredSeconds { get; }
        public float ContactSeconds { get; private set; }
        public float Fraction => Math.Min(1f, ContactSeconds / RequiredSeconds);
        public bool IsOut => ContactSeconds >= RequiredSeconds;

        public FireSuppression(float requiredSeconds)
        {
            if (float.IsNaN(requiredSeconds) || float.IsInfinity(requiredSeconds) || requiredSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(requiredSeconds));
            RequiredSeconds = requiredSeconds;
        }

        public bool ApplyWater(float dt)
        {
            if (IsOut || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return false;
            ContactSeconds = Math.Min(RequiredSeconds, ContactSeconds + dt);
            return IsOut;
        }
    }
}
