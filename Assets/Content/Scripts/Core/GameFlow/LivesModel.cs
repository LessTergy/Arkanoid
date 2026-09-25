using System;

namespace Arkanoid.Core.GameFlow
{
    public sealed class LivesModel
    {
        public const int InitialLives = 3;

        public int RemainingLives { get; private set; } = InitialLives;

        public event Action<int> LivesChanged;

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

        public void Reset()
        {
            if (RemainingLives == InitialLives)
            {
                return;
            }

            RemainingLives = InitialLives;
            LivesChanged?.Invoke(RemainingLives);
        }
    }
}
