using System;

namespace Arkanoid.Core.Bonus
{
    public sealed class BonusDropSettings
    {
        public BonusDropSettings(float chance)
        {
            if (float.IsNaN(chance) || float.IsInfinity(chance) || chance < 0f || chance > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(chance), chance,
                    "Drop chance must be finite and between 0 and 1.");
            }

            Chance = chance;
        }

        public float Chance { get; }
    }
}
