using System.Collections.Generic;
using Arkanoid.Core.GameFlow;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class LivesModelTests
    {
        [Test]
        public void AddingLives_StopsAtFiveAndPublishesOnlyUpdatedChanges()
        {
            var lives = new LivesModel();
            var changes = new List<int>();
            lives.LivesChanged += count =>
            {
                Assert.That(lives.RemainingLives, Is.EqualTo(count));
                changes.Add(count);
            };

            Assert.That(LivesModel.MaximumLives, Is.EqualTo(5));
            Assert.That(lives.TryAddLife(), Is.True);
            Assert.That(lives.TryAddLife(), Is.True);
            Assert.That(lives.TryAddLife(), Is.False);
            Assert.That(lives.RemainingLives, Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(new[] { 4, 5 }));

            Assert.That(lives.TryLoseLife(), Is.True);
            Assert.That(lives.TryAddLife(), Is.True);
            Assert.That(changes, Is.EqualTo(new[] { 4, 5, 4, 5 }));
            Assert.That(new LivesModel().RemainingLives, Is.EqualTo(3));
        }

        [Test]
        public void AddingLife_AfterLastLossDoesNotReviveOrPublish()
        {
            var lives = new LivesModel();
            while (lives.TryLoseLife())
            {
            }

            var changes = new List<int>();
            lives.LivesChanged += changes.Add;

            Assert.That(lives.TryAddLife(), Is.False);
            Assert.That(lives.RemainingLives, Is.Zero);
            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void LosingLives_StopsAtZeroAndReportsEachChange()
        {
            var lives = new LivesModel();
            var changes = new List<int>();
            lives.LivesChanged += changes.Add;

            Assert.AreEqual(3, LivesModel.InitialLives);
            Assert.AreEqual(3, lives.RemainingLives);
            Assert.IsEmpty(changes);

            Assert.IsTrue(lives.TryLoseLife());
            Assert.IsTrue(lives.TryLoseLife());
            Assert.IsTrue(lives.TryLoseLife());
            Assert.IsFalse(lives.TryLoseLife());

            Assert.AreEqual(0, lives.RemainingLives);
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, changes);
        }
    }
}
