using Arkanoid.Core.Score;

namespace Arkanoid.Tests.EditMode
{
    internal static class ScoreTestSupport
    {
        public static ComboModel CreateCombo(int count)
        {
            var combo = new ComboModel();
            for (var i = 0; i < count; i++)
            {
                combo.Advance();
            }

            return combo;
        }

        public static DoubleScoreDecorator CreateComposition(ComboModel combo, bool isEnabled)
        {
            return new DoubleScoreDecorator(new ComboScoreDecorator(new BaseScoreCalculator(), combo))
            {
                IsEnabled = isEnabled
            };
        }

        public static ComboScoreDecorator CreateReversedComposition(ComboModel combo, bool isEnabled)
        {
            var doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator())
            {
                IsEnabled = isEnabled
            };
            return new ComboScoreDecorator(doubleScore, combo);
        }

        public sealed class RecordingCalculator : IScoreCalculator
        {
            private readonly int _score;

            public RecordingCalculator(int score)
            {
                _score = score;
            }

            public int CallCount { get; private set; }
            public int LastBaseScore { get; private set; }

            public int Calculate(int baseScore)
            {
                CallCount++;
                LastBaseScore = baseScore;
                return _score;
            }
        }
    }
}
