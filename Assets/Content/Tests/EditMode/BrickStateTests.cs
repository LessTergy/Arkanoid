using Arkanoid.Core.Bricks;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BrickStateTests
    {
        [TestCase(1, 0, false)]
        [TestCase(3, 2, false)]
        [TestCase(1, 0, true)]
        public void Constructor_StartsWithDefinitionValuesAndIntactState(
            int maxHealth, int shieldCharges, bool isIndestructible)
        {
            var settings = new BrickSettings(100, maxHealth, shieldCharges, isIndestructible);
            var state = new BrickState(settings);

            Assert.That(state.Settings, Is.SameAs(settings));
            Assert.That(state.CurrentHealth, Is.EqualTo(maxHealth));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(shieldCharges));
            Assert.That(state.IsDestroyed, Is.False);
        }

        [Test]
        public void States_SharingSettings_RemainIndependentAndPreserveSettings()
        {
            var settings = new BrickSettings(200, 3, 1, false);
            var first = new BrickState(settings);
            var second = new BrickState(settings);

            Assert.That(first.TryConsumeShieldCharge(), Is.True);
            Assert.That(first.TryApplyDamage(), Is.True);

            Assert.That(first.CurrentHealth, Is.EqualTo(2));
            Assert.That(first.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(second.CurrentHealth, Is.EqualTo(3));
            Assert.That(second.CurrentShieldCharges, Is.EqualTo(1));
            Assert.That(second.IsDestroyed, Is.False);
            Assert.That(settings.MaxHealth, Is.EqualTo(3));
            Assert.That(settings.ShieldCharges, Is.EqualTo(1));
            Assert.That(settings.BaseScore, Is.EqualTo(200));
        }

        [Test]
        public void ShieldCharges_BlockDamageAndStopAtZero()
        {
            var state = new BrickState(new BrickSettings(150, 3, 2, false));

            Assert.That(state.TryApplyDamage(), Is.False);
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(2));
            Assert.That(state.CurrentHealth, Is.EqualTo(3));

            Assert.That(state.TryConsumeShieldCharge(), Is.True);
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(1));
            Assert.That(state.TryApplyDamage(), Is.False);
            Assert.That(state.CurrentHealth, Is.EqualTo(3));

            Assert.That(state.TryConsumeShieldCharge(), Is.True);
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.TryConsumeShieldCharge(), Is.False);
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(0));

            Assert.That(state.TryApplyDamage(), Is.True);
            Assert.That(state.CurrentHealth, Is.EqualTo(2));
            Assert.That(state.IsDestroyed, Is.False);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void Damage_ConsumesOneHealthAndStopsAfterDestruction(int maxHealth)
        {
            var state = new BrickState(new BrickSettings(100, maxHealth, 0, false));

            for (var hit = 1; hit <= maxHealth; hit++)
            {
                Assert.That(state.TryApplyDamage(), Is.True);
                Assert.That(state.CurrentHealth, Is.EqualTo(maxHealth - hit));
                Assert.That(state.IsDestroyed, Is.EqualTo(hit == maxHealth));
            }

            Assert.That(state.TryApplyDamage(), Is.False);
            Assert.That(state.TryConsumeShieldCharge(), Is.False);
            Assert.That(state.CurrentHealth, Is.EqualTo(0));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(state.IsDestroyed, Is.True);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void IndestructibleState_RejectsDamageAndShieldConsumption(int shieldCharges)
        {
            var state = new BrickState(new BrickSettings(0, 3, shieldCharges, true));

            Assert.That(state.TryConsumeShieldCharge(), Is.False);
            Assert.That(state.TryApplyDamage(), Is.False);
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(shieldCharges));
            Assert.That(state.IsDestroyed, Is.False);
        }
    }
}
