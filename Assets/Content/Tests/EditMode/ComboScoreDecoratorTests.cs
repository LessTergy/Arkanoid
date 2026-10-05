using System;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class ComboScoreDecoratorTests
    {
        [TestCase(100, 0, 100)]
        [TestCase(100, 1, 100)]
        [TestCase(100, 2, 200)]
        [TestCase(100, 3, 300)]
        [TestCase(100, 5, 500)]
        [TestCase(75, 3, 225)]
        [TestCase(0, 3, 0)]
        [TestCase(int.MaxValue, 0, int.MaxValue)]
        [TestCase(1073741823, 2, 2147483646)]
        [TestCase(429496729, 5, 2147483645)]
        public void Calculate_MatchesExamplesWithoutAdvancingCombo(int baseScore, int comboCount, int expectedScore)
        {
            var combo = ScoreTestSupport.CreateCombo(comboCount);
            var calculator = new ComboScoreDecorator(new BaseScoreCalculator(), combo);

            var score = calculator.Calculate(baseScore);

            Assert.That(score, Is.EqualTo(expectedScore));
            Assert.That(calculator.Calculate(baseScore), Is.EqualTo(expectedScore));
            Assert.That(combo.Count, Is.EqualTo(comboCount));
        }

        [Test]
        public void Calculate_DelegatesOriginalInputOnceAndMultipliesNestedResult()
        {
            var inner = new ScoreTestSupport.RecordingCalculator(40);
            var combo = ScoreTestSupport.CreateCombo(3);
            var calculator = new ComboScoreDecorator(inner, combo);

            var score = calculator.Calculate(75);

            Assert.That(score, Is.EqualTo(120));
            Assert.That(inner.CallCount, Is.EqualTo(1));
            Assert.That(inner.LastBaseScore, Is.EqualTo(75));
            Assert.That(combo.Count, Is.EqualTo(3));
        }

        [Test]
        public void Calculate_NegativeInput_PropagatesValidationWithoutChangingCombo()
        {
            var combo = ScoreTestSupport.CreateCombo(3);
            var calculator = new ComboScoreDecorator(new BaseScoreCalculator(), combo);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-100));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(exception.ActualValue, Is.EqualTo(-100));
            Assert.That(combo.Count, Is.EqualTo(3));
            Assert.That(calculator.Calculate(100), Is.EqualTo(300));
        }

        [TestCase(1073741824, 2)]
        [TestCase(429496730, 5)]
        public void Calculate_Overflow_ThrowsWithoutChangingCombo(int baseScore, int comboCount)
        {
            var combo = ScoreTestSupport.CreateCombo(comboCount);
            var calculator = new ComboScoreDecorator(new BaseScoreCalculator(), combo);

            Assert.Throws<OverflowException>(() => calculator.Calculate(baseScore));
            Assert.That(combo.Count, Is.EqualTo(comboCount));
        }
    }
}
