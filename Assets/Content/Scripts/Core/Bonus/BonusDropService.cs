using System;

namespace Arkanoid.Core.Bonus
{
    public sealed class BonusDropService
    {
        private readonly IRandomProvider _random;

        public BonusDropService(IRandomProvider random)
        {
            _random = random;
        }

        public bool ShouldDrop(BonusDropSettings settings)
        {
            if (settings == null || settings.Chance == 0f)
            {
                return false;
            }

            if (settings.Chance == 1f)
            {
                return true;
            }

            return NextRoll() < settings.Chance;
        }

        public bool TrySelect(BonusDropSettings settings, out int index)
        {
            index = -1;
            if (!ShouldDrop(settings))
            {
                return false;
            }

            if (settings.Weights.Count == 1)
            {
                index = 0;
                return true;
            }

            var target = NextRoll() * settings.TotalWeight;
            var cumulative = 0d;
            for (var i = 0; i < settings.Weights.Count - 1; i++)
            {
                cumulative += settings.Weights[i];
                if (target < cumulative)
                {
                    index = i;
                    return true;
                }
            }

            index = settings.Weights.Count - 1;
            return true;
        }

        private float NextRoll()
        {
            var roll = _random.NextFloat01();
            if (float.IsNaN(roll) || float.IsInfinity(roll) || roll < 0f || roll > 1f)
            {
                throw new InvalidOperationException("Random provider must return a finite value between 0 and 1.");
            }

            return roll;
        }
    }
}
