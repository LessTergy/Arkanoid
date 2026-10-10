using System;
using Arkanoid.Core.Bonus;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusDropServiceTests
    {
        [Test]
        public void WithoutProfile_RejectsDropWithoutRandom()
        {
            var random = new FakeRandomProvider();
            var service = new BonusDropService(random);

            Assert.That(service.ShouldDrop(null), Is.False);
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(0f, false)]
        [TestCase(1f, true)]
        public void EndpointChance_ReturnsGuaranteedResultWithoutRandom(float chance, bool expected)
        {
            var random = new FakeRandomProvider();
            var service = new BonusDropService(random);
            var settings = new BonusDropSettings(chance);

            Assert.That(service.ShouldDrop(settings), Is.EqualTo(expected));
            Assert.That(random.Calls, Is.Zero);
            Assert.That(settings.Chance, Is.EqualTo(chance));
        }

        [TestCase(0.25f, 0f, true)]
        [TestCase(0.25f, 0.24999999f, true)]
        [TestCase(0.25f, 0.25f, false)]
        [TestCase(0.25f, 0.25000003f, false)]
        [TestCase(0.25f, 1f, false)]
        [TestCase(float.Epsilon, 0f, true)]
        [TestCase(float.Epsilon, float.Epsilon, false)]
        [TestCase(0.99999994f, 0.99999994f, false)]
        [TestCase(0.99999994f, 1f, false)]
        public void IntermediateChance_DropsOnlyBelowChanceAndSamplesExactlyOnce(
            float chance, float roll, bool expected)
        {
            var random = new FakeRandomProvider(roll);
            var settings = new BonusDropSettings(chance);

            Assert.That(new BonusDropService(random).ShouldDrop(settings), Is.EqualTo(expected));
            Assert.That(random.Calls, Is.EqualTo(1));
            Assert.That(settings.Chance, Is.EqualTo(chance));
        }

        [TestCase(-float.Epsilon)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidChance_IsRejectedBeforeRandom(float chance)
        {
            var random = new FakeRandomProvider();
            var service = new BonusDropService(random);

            Assert.Throws<ArgumentOutOfRangeException>(() => service.ShouldDrop(new BonusDropSettings(chance)));
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(-float.Epsilon)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidRandomValue_IsRejectedAfterOneSample(float roll)
        {
            var random = new FakeRandomProvider(roll);
            var service = new BonusDropService(random);

            Assert.Throws<InvalidOperationException>(() => service.ShouldDrop(new BonusDropSettings(0.25f)));
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedAttempts_UseExactSequenceAndPreserveSettings()
        {
            var random = new FakeRandomProvider(0f, 0.25f, 1f, 0.125f);
            var service = new BonusDropService(random);
            var settings = new BonusDropSettings(0.25f);

            Assert.That(service.ShouldDrop(settings), Is.True);
            Assert.That(service.ShouldDrop(null), Is.False);
            Assert.That(service.ShouldDrop(new BonusDropSettings(0f)), Is.False);
            Assert.That(service.ShouldDrop(new BonusDropSettings(1f)), Is.True);
            Assert.That(service.ShouldDrop(settings), Is.False);
            Assert.That(service.ShouldDrop(settings), Is.False);
            Assert.That(service.ShouldDrop(settings), Is.True);
            Assert.That(random.Calls, Is.EqualTo(4));
            Assert.That(settings.Chance, Is.EqualTo(0.25f));
        }

        [Test]
        public void ProviderFailure_IsPropagatedWithoutRetry()
        {
            var random = new FakeRandomProvider();
            var service = new BonusDropService(random);

            Assert.Throws<InvalidOperationException>(() => service.ShouldDrop(new BonusDropSettings(0.25f)));
            Assert.That(random.Calls, Is.EqualTo(1));
        }
    }
}
