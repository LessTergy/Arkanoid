using System;

namespace Arkanoid.Core.Bricks
{
    public sealed class BrickSettings
    {
        public const int MinimumHealth = 1;
        public const int MaximumHealth = 5;

        public BrickSettings(int baseScore, int maxHealth, int shieldCharges, bool isIndestructible)
        {
            if (baseScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseScore), baseScore, "Base score cannot be negative.");
            }

            if (maxHealth < MinimumHealth || maxHealth > MaximumHealth)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Maximum health must be between 1 and 5.");
            }

            if (shieldCharges < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shieldCharges), shieldCharges, "Shield charges cannot be negative.");
            }

            BaseScore = baseScore;
            MaxHealth = maxHealth;
            ShieldCharges = shieldCharges;
            IsIndestructible = isIndestructible;
        }

        public int BaseScore { get; }
        public int MaxHealth { get; }
        public int ShieldCharges { get; }
        public bool IsIndestructible { get; }
    }
}
