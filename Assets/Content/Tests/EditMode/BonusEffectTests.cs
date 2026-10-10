using Arkanoid.Core.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusEffectTests
    {
        [Test]
        public void AddLife_ChangesOnlyLivesAndStopsAtMaximum()
        {
            var lives = new LivesModel();
            var doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator());
            var expansionCalls = 0;
            var context = new BonusContext(lives, doubleScore, () => expansionCalls++);
            IBonusEffect effect = new AddLifeEffect();

            effect.Apply(context);
            Assert.That(lives.RemainingLives, Is.EqualTo(4));
            effect.Apply(context);
            effect.Apply(context);

            Assert.That(lives.RemainingLives, Is.EqualTo(5));
            Assert.That(expansionCalls, Is.Zero);
            Assert.That(doubleScore.IsEnabled, Is.False);
        }

        [Test]
        public void EnableDoubleScore_UsesExistingCalculatorWithoutStackingOrAwardingScore()
        {
            var lives = new LivesModel();
            var combo = new ComboModel();
            for (var i = 0; i < 3; i++)
            {
                combo.Advance();
            }

            var doubleScore = new DoubleScoreDecorator(new ComboScoreDecorator(new BaseScoreCalculator(), combo));
            var score = new ScoreService(doubleScore);
            var expansionCalls = 0;
            var context = new BonusContext(lives, doubleScore, () => expansionCalls++);
            IBonusEffect effect = new EnableDoubleScoreEffect();
            score.AddScore(100);

            effect.Apply(context);
            effect.Apply(context);

            Assert.That(score.Total, Is.EqualTo(300));
            Assert.That(combo.Count, Is.EqualTo(3));
            Assert.That(doubleScore.IsEnabled, Is.True);
            Assert.That(expansionCalls, Is.Zero);
            Assert.That(lives.RemainingLives, Is.EqualTo(3));

            score.AddScore(100);
            Assert.That(score.Total, Is.EqualTo(900));
        }

        [Test]
        public void ExpandPaddle_InvokesOperationOncePerApplyWithoutChangingOtherState()
        {
            var lives = new LivesModel();
            var doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator());
            var calls = 0;
            var context = new BonusContext(lives, doubleScore, () => calls++);
            IBonusEffect effect = new ExpandPaddleEffect();

            effect.Apply(context);
            Assert.That(calls, Is.EqualTo(1));
            effect.Apply(context);

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(lives.RemainingLives, Is.EqualTo(LivesModel.InitialLives));
            Assert.That(doubleScore.IsEnabled, Is.False);
        }
    }
}
