using System;
using Arkanoid.Core.Bonus;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusDropServiceTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.24999999f, 0)]
        [TestCase(0.25f, 1)]
        [TestCase(0.25000003f, 1)]
        [TestCase(1f, 1)]
        public void WeightedSelection_UsesStrictBoundaryAndInclusiveEndpoints(float roll, int expected)
        {
            var random = new FakeRandomProvider(roll);
            var settings = new BonusDropSettings(1f, new[] { 1f, 3f });

            Assert.That(new BonusDropService(random).TrySelect(settings, out var index), Is.True);
            Assert.That(index, Is.EqualTo(expected));
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        [TestCase(0f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(1f, 1)]
        public void LargeFiniteWeights_DoNotOverflowSelection(float roll, int expected)
        {
            var random = new FakeRandomProvider(roll);
            var settings = new BonusDropSettings(1f, new[] { float.MaxValue, float.MaxValue });

            Assert.That(new BonusDropService(random).TrySelect(settings, out var index), Is.True);
            Assert.That(index, Is.EqualTo(expected));
        }

        [TestCase(0f, 0f, false, -1, 0)]
        [TestCase(1f, 1f, true, 0, 0)]
        [TestCase(0.25f, 0.125f, true, 0, 1)]
        [TestCase(0.25f, 0.25f, false, -1, 1)]
        public void SingleEntry_PreservesStage7RandomConsumption(
            float chance, float roll, bool expected, int expectedIndex, int expectedCalls)
        {
            var random = new FakeRandomProvider(roll);
            var service = new BonusDropService(random);

            Assert.That(service.TrySelect(new BonusDropSettings(chance, new[] { 1f }), out var index), Is.EqualTo(expected));
            Assert.That(index, Is.EqualTo(expectedIndex));
            Assert.That(random.Calls, Is.EqualTo(expectedCalls));
        }

        [Test]
        public void Selection_ProbabilityRollPrecedesChoiceAndFailureSkipsChoice()
        {
            var random = new FakeRandomProvider(0.25f, 0.125f, 0.25f);
            var service = new BonusDropService(random);
            var settings = new BonusDropSettings(0.25f, new[] { 1f, 3f });

            Assert.That(service.TrySelect(settings, out var rejected), Is.False);
            Assert.That(rejected, Is.EqualTo(-1));
            Assert.That(random.Calls, Is.EqualTo(1));
            Assert.That(service.TrySelect(settings, out var selected), Is.True);
            Assert.That(selected, Is.EqualTo(1));
            Assert.That(random.Calls, Is.EqualTo(3));
        }

        [Test]
        public void AbsentAndZeroChanceTables_DoNotSelectOrSample()
        {
            var random = new FakeRandomProvider();
            var service = new BonusDropService(random);

            Assert.That(service.TrySelect(null, out var absent), Is.False);
            Assert.That(service.TrySelect(new BonusDropSettings(0f, new[] { 1f, 3f }), out var zero), Is.False);
            Assert.That(absent, Is.EqualTo(-1));
            Assert.That(zero, Is.EqualTo(-1));
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(-float.Epsilon)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidSelectionRoll_IsRejectedWithoutRetry(float roll)
        {
            var random = new FakeRandomProvider(0f, roll);
            var service = new BonusDropService(random);

            Assert.Throws<InvalidOperationException>(() => service.TrySelect(
                new BonusDropSettings(0.25f, new[] { 1f, 3f }), out _));
            Assert.That(random.Calls, Is.EqualTo(2));
        }

        [Test]
        public void SelectionProviderFailure_IsPropagatedWithoutRetry()
        {
            var random = new FakeRandomProvider(0f);
            var service = new BonusDropService(random);

            Assert.Throws<InvalidOperationException>(() => service.TrySelect(
                new BonusDropSettings(0.25f, new[] { 1f, 3f }), out _));
            Assert.That(random.Calls, Is.EqualTo(2));
        }

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
            var settings = new BonusDropSettings(chance, new[] { 1f });

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
            var settings = new BonusDropSettings(chance, new[] { 1f });

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

            Assert.Throws<ArgumentOutOfRangeException>(() => service.ShouldDrop(new BonusDropSettings(chance, new[] { 1f })));
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

            Assert.Throws<InvalidOperationException>(() => service.ShouldDrop(new BonusDropSettings(0.25f, new[] { 1f })));
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedAttempts_UseExactSequenceAndPreserveSettings()
        {
            var random = new FakeRandomProvider(0f, 0.25f, 1f, 0.125f);
            var service = new BonusDropService(random);
            var settings = new BonusDropSettings(0.25f, new[] { 1f });

            Assert.That(service.ShouldDrop(settings), Is.True);
            Assert.That(service.ShouldDrop(null), Is.False);
            Assert.That(service.ShouldDrop(new BonusDropSettings(0f, new[] { 1f })), Is.False);
            Assert.That(service.ShouldDrop(new BonusDropSettings(1f, new[] { 1f })), Is.True);
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

            Assert.Throws<InvalidOperationException>(() => service.ShouldDrop(new BonusDropSettings(0.25f, new[] { 1f })));
            Assert.That(random.Calls, Is.EqualTo(1));
        }
    }
}
