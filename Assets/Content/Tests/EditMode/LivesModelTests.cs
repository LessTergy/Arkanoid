using System.Collections.Generic;
using Arkanoid.Core.GameFlow;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class LivesModelTests
    {
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
