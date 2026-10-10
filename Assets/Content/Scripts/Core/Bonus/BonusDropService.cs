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

            var roll = _random.NextFloat01();
            if (float.IsNaN(roll) || float.IsInfinity(roll) || roll < 0f || roll > 1f)
            {
                throw new InvalidOperationException("Random provider must return a finite value between 0 and 1.");
            }

            return roll < settings.Chance;
        }
    }
}
