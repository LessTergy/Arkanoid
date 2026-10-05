using System;

namespace Arkanoid.Core.Score
{
    public sealed class ComboModel
    {
        private const int MaximumCount = 5;

        public int Count { get; private set; }
        public event Action<int> ComboChanged;

        public int Multiplier => Math.Max(1, Count);

        public void Advance()
        {
            SetCount(Math.Min(Count + 1, MaximumCount));
        }

        public void Reset()
        {
            SetCount(0);
        }

        private void SetCount(int count)
        {
            if (Count == count)
            {
                return;
            }

            Count = count;
            ComboChanged?.Invoke(Count);
        }
    }
}
