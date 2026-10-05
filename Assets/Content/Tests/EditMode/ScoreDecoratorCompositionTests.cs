using System;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class ScoreDecoratorCompositionTests
    {
        [TestCase(100, 0, false, 100)]
        [TestCase(100, 0, true, 200)]
        [TestCase(100, 1, false, 100)]
        [TestCase(100, 1, true, 200)]
        [TestCase(100, 2, false, 200)]
        [TestCase(100, 2, true, 400)]
        [TestCase(100, 3, false, 300)]
        [TestCase(100, 3, true, 600)]
        [TestCase(100, 5, false, 500)]
        [TestCase(100, 5, true, 1000)]
        [TestCase(75, 3, false, 225)]
        [TestCase(75, 3, true, 450)]
        [TestCase(0, 3, false, 0)]
        [TestCase(0, 3, true, 0)]
        [TestCase(int.MaxValue, 0, false, int.MaxValue)]
        [TestCase(214748364, 5, true, 2147483640)]
        public void Calculate_CompositionsMatchExamplesAndOrderDoesNotChangeResult(
            int baseScore, int comboCount, bool isEnabled, int expectedScore)
        {
            var combo = ScoreTestSupport.CreateCombo(comboCount);
            var calculator = ScoreTestSupport.CreateComposition(combo, isEnabled);
            var reversedCalculator = ScoreTestSupport.CreateReversedComposition(combo, isEnabled);

            var score = calculator.Calculate(baseScore);
            var reversedScore = reversedCalculator.Calculate(baseScore);

            Assert.That(score, Is.EqualTo(expectedScore));
            Assert.That(reversedScore, Is.EqualTo(expectedScore));
            Assert.That(calculator.Calculate(baseScore), Is.EqualTo(expectedScore));
            Assert.That(combo.Count, Is.EqualTo(comboCount));
            Assert.That(calculator.IsEnabled, Is.EqualTo(isEnabled));
        }

        [Test]
        public void Calculate_CompositionDelegatesOriginalInputOnceAndUsesNestedResult()
        {
            var inner = new ScoreTestSupport.RecordingCalculator(40);
            var combo = ScoreTestSupport.CreateCombo(3);
            var calculator = new DoubleScoreDecorator(new ComboScoreDecorator(inner, combo))
            {
                IsEnabled = true
            };

            var score = calculator.Calculate(75);

            Assert.That(score, Is.EqualTo(240));
            Assert.That(inner.CallCount, Is.EqualTo(1));
            Assert.That(inner.LastBaseScore, Is.EqualTo(75));
            Assert.That(combo.Count, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Calculate_NegativeInput_BothOrdersPropagateValidationAndPreserveState(bool isEnabled)
        {
            var combo = ScoreTestSupport.CreateCombo(3);
            var calculator = ScoreTestSupport.CreateComposition(combo, isEnabled);
            var reversedCalculator = ScoreTestSupport.CreateReversedComposition(combo, isEnabled);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-100));
            var reversedException = Assert.Throws<ArgumentOutOfRangeException>(() => reversedCalculator.Calculate(-100));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(reversedException.ParamName, Is.EqualTo("baseScore"));
            Assert.That(combo.Count, Is.EqualTo(3));
            Assert.That(calculator.IsEnabled, Is.EqualTo(isEnabled));
        }

        [TestCase(1073741824, 2, false)]
        [TestCase(1073741824, 0, true)]
        [TestCase(214748365, 5, true)]
        public void Calculate_Overflow_BothOrdersThrowAndPreserveState(int baseScore, int comboCount, bool isEnabled)
        {
            var combo = ScoreTestSupport.CreateCombo(comboCount);
            var calculator = ScoreTestSupport.CreateComposition(combo, isEnabled);
            var reversedCalculator = ScoreTestSupport.CreateReversedComposition(combo, isEnabled);

            Assert.Throws<OverflowException>(() => calculator.Calculate(baseScore));
            Assert.Throws<OverflowException>(() => reversedCalculator.Calculate(baseScore));
            Assert.That(combo.Count, Is.EqualTo(comboCount));
            Assert.That(calculator.IsEnabled, Is.EqualTo(isEnabled));
        }

        [Test]
        public void Calculate_ReusesCurrentComboAndEnabledStateWithoutRebuildingComposition()
        {
            var combo = ScoreTestSupport.CreateCombo(1);
            var calculator = ScoreTestSupport.CreateComposition(combo, false);

            Assert.That(calculator.Calculate(100), Is.EqualTo(100));

            combo.Advance();
            Assert.That(calculator.Calculate(100), Is.EqualTo(200));

            calculator.IsEnabled = true;
            Assert.That(calculator.Calculate(100), Is.EqualTo(400));

            combo.Reset();
            Assert.That(calculator.Calculate(100), Is.EqualTo(200));

            calculator.IsEnabled = false;
            Assert.That(calculator.Calculate(100), Is.EqualTo(100));
            Assert.That(combo.Count, Is.EqualTo(0));
        }
    }
}
