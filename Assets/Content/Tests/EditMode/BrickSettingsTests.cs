using System;
using Arkanoid.Core.Bricks;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BrickSettingsTests
    {
        [TestCase(100, 1, 0, false)]
        [TestCase(200, 3, 0, false)]
        [TestCase(150, 1, 1, false)]
        [TestCase(0, 1, 0, true)]
        [TestCase(0, 3, 2, false)]
        [TestCase(0, 5, 0, false)]
        [TestCase(int.MaxValue, 5, int.MaxValue, true)]
        public void Constructor_ValidData_PreservesSettings(
            int baseScore, int maxHealth, int shieldCharges, bool isIndestructible)
        {
            var settings = new BrickSettings(baseScore, maxHealth, shieldCharges, isIndestructible);

            Assert.That(settings.BaseScore, Is.EqualTo(baseScore));
            Assert.That(settings.MaxHealth, Is.EqualTo(maxHealth));
            Assert.That(settings.ShieldCharges, Is.EqualTo(shieldCharges));
            Assert.That(settings.IsIndestructible, Is.EqualTo(isIndestructible));
        }

        [TestCase(-1, 1, 0, "baseScore")]
        [TestCase(100, 0, 0, "maxHealth")]
        [TestCase(100, -1, 0, "maxHealth")]
        [TestCase(100, 6, 0, "maxHealth")]
        [TestCase(100, int.MaxValue, 0, "maxHealth")]
        [TestCase(100, 1, -1, "shieldCharges")]
        public void Constructor_InvalidData_ThrowsBeforeCreatingSettings(
            int baseScore, int maxHealth, int shieldCharges, string parameterName)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrickSettings(baseScore, maxHealth, shieldCharges, false));

            Assert.That(exception.ParamName, Is.EqualTo(parameterName));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void Constructor_IndestructibleStillRequiresHealthInRange(int maxHealth)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrickSettings(0, maxHealth, 0, true));

            Assert.That(exception.ParamName, Is.EqualTo("maxHealth"));
        }
    }
}
