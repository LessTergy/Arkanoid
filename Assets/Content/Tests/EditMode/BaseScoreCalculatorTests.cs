using System;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BaseScoreCalculatorTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(75)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void Calculate_NonNegativeBaseScore_ReturnsUnchangedValue(int baseScore)
        {
            var calculator = new BaseScoreCalculator();

            var score = calculator.Calculate(baseScore);

            Assert.That(score, Is.EqualTo(baseScore));
        }

        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(int.MinValue)]
        public void Calculate_NegativeBaseScore_ThrowsAndAllowsNextCalculation(int baseScore)
        {
            var calculator = new BaseScoreCalculator();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(baseScore));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(exception.ActualValue, Is.EqualTo(baseScore));
            Assert.That(calculator.Calculate(100), Is.EqualTo(100));
        }
    }
}
