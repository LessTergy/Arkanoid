using System;

namespace Arkanoid.Core.GameFlow
{
    public sealed class LivesModel
    {
        public const int InitialLives = 3;
        public const int MaximumLives = 5;

        public int RemainingLives { get; private set; } = InitialLives;

        public event Action<int> LivesChanged;

        public bool TryAddLife()
        {
            if (RemainingLives == 0 || RemainingLives == MaximumLives)
            {
                return false;
            }

            RemainingLives++;
            LivesChanged?.Invoke(RemainingLives);
            return true;
        }

        public bool TryLoseLife()
        {
            if (RemainingLives == 0)
            {
                return false;
            }

            RemainingLives--;
            LivesChanged?.Invoke(RemainingLives);
            return true;
        }
    }
}
