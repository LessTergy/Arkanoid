namespace Arkanoid.Core.Score
{
    public sealed class DoubleScoreDecorator : IScoreCalculator
    {
        private readonly IScoreCalculator _inner;

        public DoubleScoreDecorator(IScoreCalculator inner)
        {
            _inner = inner;
        }

        public bool IsEnabled { get; set; }

        public int Calculate(int baseScore)
        {
            var score = _inner.Calculate(baseScore);
            if (!IsEnabled)
            {
                return score;
            }

            return checked(score * 2);
        }
    }
}
