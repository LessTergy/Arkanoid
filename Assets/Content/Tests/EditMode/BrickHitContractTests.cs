using System;
using Arkanoid.Core.Bricks;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BrickHitContractTests
    {
        [Test]
        public void Request_ReferencesTargetWithoutChangingIt()
        {
            var state = new BrickState(new BrickSettings(200, 3, 1, false));

            var request = new BrickHitRequest(state);

            Assert.That(request.State, Is.SameAs(state));
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(1));
            Assert.That(state.IsDestroyed, Is.False);
        }

        [Test]
        public void Result_CapturesShieldBreakWithoutChangingHealth()
        {
            var state = new BrickState(new BrickSettings(150, 3, 1, false));
            Assert.That(state.TryConsumeShieldCharge(), Is.True);

            var result = new BrickHitResult(BrickHitOutcome.ShieldBroken, state);

            Assert.That(result.Outcome, Is.EqualTo(BrickHitOutcome.ShieldBroken));
            Assert.That(result.CurrentHealth, Is.EqualTo(3));
            Assert.That(result.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(state.IsDestroyed, Is.False);
        }

        [Test]
        public void ResultSnapshot_RemainsUnchangedAfterLaterDamage()
        {
            var state = new BrickState(new BrickSettings(200, 3, 0, false));
            Assert.That(state.TryApplyDamage(), Is.True);
            var result = new BrickHitResult(BrickHitOutcome.Damaged, state);

            Assert.That(state.TryApplyDamage(), Is.True);

            Assert.That(result.Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(result.CurrentHealth, Is.EqualTo(2));
            Assert.That(result.CurrentShieldCharges, Is.EqualTo(0));
            Assert.That(state.CurrentHealth, Is.EqualTo(1));
        }

        [Test]
        public void DestroyedAndIgnored_AreDistinctAtTheSameZeroHealth()
        {
            var state = new BrickState(new BrickSettings(0, 1, 0, false));
            Assert.That(state.TryApplyDamage(), Is.True);
            var destruction = new BrickHitResult(BrickHitOutcome.Destroyed, state);

            Assert.That(state.TryApplyDamage(), Is.False);
            var repeatedHit = new BrickHitResult(BrickHitOutcome.Ignored, state);

            Assert.That(destruction.Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(repeatedHit.Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
            Assert.That(destruction.CurrentHealth, Is.EqualTo(0));
            Assert.That(repeatedHit.CurrentHealth, Is.EqualTo(0));
            Assert.That(state.Settings.BaseScore, Is.EqualTo(0));
        }

        [TestCase(-1)]
        [TestCase(100)]
        public void Result_InvalidOutcome_ThrowsWithoutChangingState(int outcomeValue)
        {
            var state = new BrickState(new BrickSettings(100, 1, 1, false));

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrickHitResult((BrickHitOutcome)outcomeValue, state));

            Assert.That(exception.ParamName, Is.EqualTo("outcome"));
            Assert.That(state.CurrentHealth, Is.EqualTo(1));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(1));
            Assert.That(state.IsDestroyed, Is.False);
        }
    }
}
