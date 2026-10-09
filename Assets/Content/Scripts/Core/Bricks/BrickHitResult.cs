using System;

namespace Arkanoid.Core.Bricks
{
    public readonly struct BrickHitResult
    {
        public BrickHitResult(BrickHitOutcome outcome, BrickState state)
        {
            if (!Enum.IsDefined(typeof(BrickHitOutcome), outcome))
            {
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported brick hit outcome.");
            }

            Outcome = outcome;
            CurrentHealth = state.CurrentHealth;
            CurrentShieldCharges = state.CurrentShieldCharges;
        }

        public BrickHitOutcome Outcome { get; }
        public int CurrentHealth { get; }
        public int CurrentShieldCharges { get; }
    }
}
