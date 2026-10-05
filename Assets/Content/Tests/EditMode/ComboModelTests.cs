using System.Collections.Generic;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class ComboModelTests
    {
        [Test]
        public void NewModel_HasZeroCountAndNeutralMultiplier()
        {
            var combo = new ComboModel();

            Assert.That(combo.Count, Is.EqualTo(0));
            Assert.That(combo.Multiplier, Is.EqualTo(1));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        [TestCase(100, 5)]
        public void Advance_IncreasesComboUpToFive(int destructionCount, int expectedCombo)
        {
            var combo = new ComboModel();

            for (var i = 0; i < destructionCount; i++)
            {
                combo.Advance();
            }

            Assert.That(combo.Count, Is.EqualTo(expectedCombo));
            Assert.That(combo.Multiplier, Is.EqualTo(expectedCombo));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(8)]
        public void Reset_ClearsComboAndNextDestructionStartsAtOne(int destructionCount)
        {
            var combo = new ComboModel();
            for (var i = 0; i < destructionCount; i++)
            {
                combo.Advance();
            }

            combo.Reset();

            Assert.That(combo.Count, Is.EqualTo(0));
            Assert.That(combo.Multiplier, Is.EqualTo(1));

            combo.Advance();

            Assert.That(combo.Count, Is.EqualTo(1));
            Assert.That(combo.Multiplier, Is.EqualTo(1));
        }

        [Test]
        public void ReadingMultiplier_DoesNotAdvanceCombo()
        {
            var combo = new ComboModel();
            combo.Advance();
            combo.Advance();

            var firstMultiplier = combo.Multiplier;
            var secondMultiplier = combo.Multiplier;

            Assert.That(firstMultiplier, Is.EqualTo(2));
            Assert.That(secondMultiplier, Is.EqualTo(2));
            Assert.That(combo.Count, Is.EqualTo(2));
        }

        [Test]
        public void ComboChanged_PublishesUpdatedCountOnlyWhenCountChanges()
        {
            var combo = new ComboModel();
            var published = new List<int>();
            combo.ComboChanged += count =>
            {
                Assert.That(combo.Count, Is.EqualTo(count));
                published.Add(count);
            };

            combo.Reset();
            Assert.That(published, Is.Empty);

            for (var i = 0; i < 6; i++)
            {
                combo.Advance();
            }

            combo.Reset();
            combo.Reset();

            Assert.That(published, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 0 }));
            Assert.That(combo.Count, Is.EqualTo(0));
        }
    }
}
