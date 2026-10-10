using System;
using Arkanoid.Core.Bonus;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusDropSettingsTests
    {
        [Test]
        public void Constructor_CopiesWeightsAndAccumulatesInDouble()
        {
            var weights = new[] { float.MaxValue, float.MaxValue };
            var settings = new BonusDropSettings(0.25f, weights);
            weights[0] = 1f;

            Assert.That(settings.Weights, Is.EqualTo(new[] { float.MaxValue, float.MaxValue }));
            Assert.That(settings.TotalWeight, Is.EqualTo((double)float.MaxValue * 2d));
            Assert.That(double.IsInfinity(settings.TotalWeight), Is.False);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<float>)settings.Weights)[0] = 1f);
        }

        [Test]
        public void Constructor_NullOrEmptyTable_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new BonusDropSettings(0f, null));
            Assert.Throws<ArgumentException>(() => new BonusDropSettings(0f, Array.Empty<float>()));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidWeight_IsRejectedEvenAtZeroChance(float weight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BonusDropSettings(0f, new[] { 1f, weight }));
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(1f)]
        public void Constructor_ValidChance_PreservesValue(float chance)
        {
            Assert.That(new BonusDropSettings(chance, new[] { 1f }).Chance, Is.EqualTo(chance));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidChance_Throws(float chance)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BonusDropSettings(chance, new[] { 1f }));

            Assert.That(exception.ParamName, Is.EqualTo("chance"));
        }
    }
}
