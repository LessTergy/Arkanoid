using System;

namespace Arkanoid.Core.Score
{
    public sealed class BaseScoreCalculator : IScoreCalculator
    {
        public int Calculate(int baseScore)
        {
            if (baseScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseScore), baseScore, "Base score cannot be negative.");
            }

            return baseScore;
        }
    }
}
