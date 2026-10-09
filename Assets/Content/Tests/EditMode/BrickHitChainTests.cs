using Arkanoid.Core.Bricks;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BrickHitChainTests
    {
        [TestCase(0)]
        [TestCase(2)]
        public void Indestructible_StopsBeforeNextAndPreservesBothValues(int shieldCharges)
        {
            var state = new BrickState(new BrickSettings(0, 3, shieldCharges, true));
            var next = new RecordingHandler();
            var handler = new IndestructibleHitHandler(next);

            var result = handler.Handle(new BrickHitRequest(state));

            AssertResult(result, BrickHitOutcome.Indestructible, 3, shieldCharges);
            Assert.That(next.Calls, Is.Zero);
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(shieldCharges));
        }

        [Test]
        public void Indestructible_WhenDestructible_ForwardsSameRequestAndResultOnce()
        {
            AssertDelegation(next => new IndestructibleHitHandler(next));
        }

        [Test]
        public void Shield_WhenAbsent_ForwardsSameRequestAndResultOnce()
        {
            AssertDelegation(next => new ShieldHitHandler(next));
        }

        [TestCase(1, BrickHitOutcome.ShieldBroken)]
        [TestCase(2, BrickHitOutcome.ShieldAbsorbed)]
        public void Shield_StopsBeforeNextIncludingLastCharge(int charges, BrickHitOutcome outcome)
        {
            var state = new BrickState(new BrickSettings(150, 3, charges, false));
            var next = new RecordingHandler();
            var handler = new ShieldHitHandler(next);

            var result = handler.Handle(new BrickHitRequest(state));

            AssertResult(result, outcome, 3, charges - 1);
            Assert.That(next.Calls, Is.Zero);
            Assert.That(state.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentShieldCharges, Is.EqualTo(charges - 1));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void Damage_RemovesOneHealthPerHitAndDestroysOnce(int health)
        {
            var state = new BrickState(new BrickSettings(100, health, 0, false));
            var request = new BrickHitRequest(state);
            var handler = new DamageHitHandler();

            for (var hit = 1; hit <= health; hit++)
            {
                var outcome = hit == health ? BrickHitOutcome.Destroyed : BrickHitOutcome.Damaged;
                AssertResult(handler.Handle(request), outcome, health - hit, 0);
            }

            AssertResult(handler.Handle(request), BrickHitOutcome.Ignored, 0, 0);
        }

        [TestCase(1, false)]
        [TestCase(0, true)]
        public void Damage_WhenStateRejectsDamage_DoesNotReportDestruction(int shields, bool indestructible)
        {
            var state = new BrickState(new BrickSettings(100, 1, shields, indestructible));

            AssertResult(new DamageHitHandler().Handle(new BrickHitRequest(state)),
                BrickHitOutcome.Ignored, 1, shields);
            Assert.That(state.IsDestroyed, Is.False);
        }

        [Test]
        public void Processor_AlreadyDestroyed_DoesNotEnterChain()
        {
            var state = new BrickState(new BrickSettings(100, 1, 0, false));
            state.TryApplyDamage();
            var head = new RecordingHandler();

            var result = new BrickHitProcessor(head).Process(new BrickHitRequest(state));

            AssertResult(result, BrickHitOutcome.Ignored, 0, 0);
            Assert.That(head.Calls, Is.Zero);
        }

        [Test]
        public void Processor_LiveState_ForwardsSameRequestAndResultOnce()
        {
            var state = new BrickState(new BrickSettings(100, 3, 0, false));
            var request = new BrickHitRequest(state);
            var head = new RecordingHandler();

            var result = new BrickHitProcessor(head).Process(request);

            Assert.That(head.Calls, Is.EqualTo(1));
            Assert.That(head.Request, Is.SameAs(request));
            AssertResult(result, BrickHitOutcome.Damaged, 2, 0);
        }

        [Test]
        public void Chain_ShieldedDurable_ConsumesShellsBeforeHealthAndPreservesSettings()
        {
            var settings = new BrickSettings(200, 3, 2, false);
            var state = new BrickState(settings);
            var request = new BrickHitRequest(state);
            var processor = CreateProcessor();

            AssertResult(processor.Process(request), BrickHitOutcome.ShieldAbsorbed, 3, 1);
            AssertResult(processor.Process(request), BrickHitOutcome.ShieldBroken, 3, 0);
            AssertResult(processor.Process(request), BrickHitOutcome.Damaged, 2, 0);
            AssertResult(processor.Process(request), BrickHitOutcome.Damaged, 1, 0);
            AssertResult(processor.Process(request), BrickHitOutcome.Destroyed, 0, 0);
            AssertResult(processor.Process(request), BrickHitOutcome.Ignored, 0, 0);
            Assert.That(settings.MaxHealth, Is.EqualTo(3));
            Assert.That(settings.ShieldCharges, Is.EqualTo(2));
            Assert.That(settings.BaseScore, Is.EqualTo(200));
        }

        [Test]
        public void Chain_IndestructibleWithShield_StopsBeforeAnyMutation()
        {
            var state = new BrickState(new BrickSettings(0, 3, 2, true));
            var request = new BrickHitRequest(state);
            var processor = CreateProcessor();

            for (var hit = 0; hit < 5; hit++)
            {
                AssertResult(processor.Process(request), BrickHitOutcome.Indestructible, 3, 2);
            }
        }

        [Test]
        public void SharedChain_KeepsStatesIndependent()
        {
            var settings = new BrickSettings(150, 1, 1, false);
            var first = new BrickState(settings);
            var second = new BrickState(settings);
            var processor = CreateProcessor();

            AssertResult(processor.Process(new BrickHitRequest(first)), BrickHitOutcome.ShieldBroken, 1, 0);
            AssertResult(processor.Process(new BrickHitRequest(first)), BrickHitOutcome.Destroyed, 0, 0);
            Assert.That(second.CurrentHealth, Is.EqualTo(1));
            Assert.That(second.CurrentShieldCharges, Is.EqualTo(1));
            AssertResult(processor.Process(new BrickHitRequest(second)), BrickHitOutcome.ShieldBroken, 1, 0);
        }

        private static void AssertDelegation(System.Func<IBrickHitHandler, IBrickHitHandler> createHandler)
        {
            var state = new BrickState(new BrickSettings(100, 3, 0, false));
            var request = new BrickHitRequest(state);
            var next = new RecordingHandler();

            var result = createHandler(next).Handle(request);

            Assert.That(next.Calls, Is.EqualTo(1));
            Assert.That(next.Request, Is.SameAs(request));
            AssertResult(result, BrickHitOutcome.Damaged, 2, 0);
            Assert.That(state.CurrentHealth, Is.EqualTo(2));
        }

        private static BrickHitProcessor CreateProcessor()
        {
            return new BrickHitProcessor(new IndestructibleHitHandler(new ShieldHitHandler(new DamageHitHandler())));
        }

        private static void AssertResult(BrickHitResult result, BrickHitOutcome outcome, int health, int shields)
        {
            Assert.That(result.Outcome, Is.EqualTo(outcome));
            Assert.That(result.CurrentHealth, Is.EqualTo(health));
            Assert.That(result.CurrentShieldCharges, Is.EqualTo(shields));
        }

        private sealed class RecordingHandler : IBrickHitHandler
        {
            public int Calls { get; private set; }
            public BrickHitRequest Request { get; private set; }

            public BrickHitResult Handle(BrickHitRequest request)
            {
                Calls++;
                Request = request;
                request.State.TryApplyDamage();
                return new BrickHitResult(BrickHitOutcome.Damaged, request.State);
            }
        }
    }
}
