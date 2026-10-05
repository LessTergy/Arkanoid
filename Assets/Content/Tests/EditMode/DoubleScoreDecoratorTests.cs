using System;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class DoubleScoreDecoratorTests
    {
        [TestCase(0, false, 0)]
        [TestCase(0, true, 0)]
        [TestCase(75, false, 75)]
        [TestCase(75, true, 150)]
        [TestCase(100, false, 100)]
        [TestCase(100, true, 200)]
        [TestCase(int.MaxValue, false, int.MaxValue)]
        [TestCase(1073741823, true, 2147483646)]
        public void Calculate_MatchesExamples(int baseScore, bool isEnabled, int expectedScore)
        {
            var calculator = new DoubleScoreDecorator(new BaseScoreCalculator())
            {
                IsEnabled = isEnabled
            };

            var score = calculator.Calculate(baseScore);

            Assert.That(score, Is.EqualTo(expectedScore));
            Assert.That(calculator.IsEnabled, Is.EqualTo(isEnabled));
        }

        [Test]
        public void NewDecorator_HasDoubleScoreDisabled()
        {
            var calculator = new DoubleScoreDecorator(new BaseScoreCalculator());

            Assert.That(calculator.IsEnabled, Is.False);
            Assert.That(calculator.Calculate(100), Is.EqualTo(100));
        }

        [TestCase(false, 40)]
        [TestCase(true, 80)]
        public void Calculate_DelegatesOriginalInputOnceAndUsesNestedResult(bool isEnabled, int expectedScore)
        {
            var inner = new ScoreTestSupport.RecordingCalculator(40);
            var calculator = new DoubleScoreDecorator(inner)
            {
                IsEnabled = isEnabled
            };

            var score = calculator.Calculate(75);

            Assert.That(score, Is.EqualTo(expectedScore));
            Assert.That(inner.CallCount, Is.EqualTo(1));
            Assert.That(inner.LastBaseScore, Is.EqualTo(75));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Calculate_NegativeInput_PropagatesValidationWithoutChangingEnabledState(bool isEnabled)
        {
            var calculator = new DoubleScoreDecorator(new BaseScoreCalculator())
            {
                IsEnabled = isEnabled
            };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-100));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(exception.ActualValue, Is.EqualTo(-100));
            Assert.That(calculator.IsEnabled, Is.EqualTo(isEnabled));
        }

        [Test]
        public void Calculate_EnabledDoubleScoreOverflow_ThrowsWithoutDisablingEffect()
        {
            var calculator = new DoubleScoreDecorator(new BaseScoreCalculator())
            {
                IsEnabled = true
            };

            Assert.Throws<OverflowException>(() => calculator.Calculate(1073741824));
            Assert.That(calculator.IsEnabled, Is.True);
        }

        [Test]
        public void EnableAgainAndDisable_DoNotStackOrRequireNewCalculator()
        {
            var calculator = new DoubleScoreDecorator(new BaseScoreCalculator());

            calculator.IsEnabled = true;
            Assert.That(calculator.Calculate(100), Is.EqualTo(200));

            calculator.IsEnabled = true;
            Assert.That(calculator.Calculate(100), Is.EqualTo(200));

            calculator.IsEnabled = false;
            Assert.That(calculator.Calculate(100), Is.EqualTo(100));
        }
    }
}
