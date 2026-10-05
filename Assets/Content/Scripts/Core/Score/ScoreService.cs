using System;

namespace Arkanoid.Core.Score
{
    public sealed class ScoreService
    {
        private readonly IScoreCalculator _calculator;

        public ScoreService(IScoreCalculator calculator)
        {
            _calculator = calculator;
        }

        public int Total { get; private set; }
        public event Action<int> ScoreChanged;

        public void AddScore(int baseScore)
        {
            var score = _calculator.Calculate(baseScore);
            Total = checked(Total + score);
            ScoreChanged?.Invoke(Total);
        }
    }
}
