using System;
using Arkanoid.Core.Bonus;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusDropSettingsTests
    {
        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(1f)]
        public void Constructor_ValidChance_PreservesValue(float chance)
        {
            Assert.That(new BonusDropSettings(chance).Chance, Is.EqualTo(chance));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidChance_Throws(float chance)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new BonusDropSettings(chance));

            Assert.That(exception.ParamName, Is.EqualTo("chance"));
        }
    }
}
