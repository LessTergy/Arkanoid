using Arkanoid.Core.Score;
using VContainer;

namespace Arkanoid.Bonus
{
    public sealed class EnableDoubleScoreEffect : BonusEffect
    {
        private DoubleScoreDecorator _doubleScore;

        [Inject]
        public void Construct(DoubleScoreDecorator doubleScore)
        {
            _doubleScore = doubleScore;
        }

        public override void Apply()
        {
            _doubleScore.IsEnabled = true;
        }
    }
}
