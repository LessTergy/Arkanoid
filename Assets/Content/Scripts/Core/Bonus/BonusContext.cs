using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;

namespace Arkanoid.Core.Bonus
{
    public sealed class BonusContext
    {
        public BonusContext(LivesModel lives, DoubleScoreDecorator doubleScore, Action expandPaddle)
        {
            Lives = lives;
            DoubleScore = doubleScore;
            ExpandPaddle = expandPaddle;
        }

        public LivesModel Lives { get; }
        public DoubleScoreDecorator DoubleScore { get; }
        public Action ExpandPaddle { get; }
    }
}
