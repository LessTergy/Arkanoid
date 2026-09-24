using Arkanoid.Input;
using Arkanoid.Paddle;
using NUnit.Framework;
using UnityEngine;

namespace Arkanoid.Tests.EditMode
{
    public sealed class PaddlePositionCalculatorTests
    {
        private static readonly Rect Bounds = new Rect(-5.4f, -8.6f, 10.8f, 19.2f);

        [Test]
        public void DirectionalInput_UsesSpeedAndPhysicsDeltaTime()
        {
            var move = new PlayerMoveIntent(1f, null);

            var nextX = PaddlePositionCalculator.CalculateNextX(0f, move, 8f, 0.02f, 2f, Bounds);

            Assert.AreEqual(0.16f, nextX, 0.0001f);
        }

        [Test]
        public void DirectionalInput_ClampsCenterAtLeftBoundary()
        {
            var move = new PlayerMoveIntent(-1f, null);

            var nextX = PaddlePositionCalculator.CalculateNextX(-4.35f, move, 8f, 0.02f, 2f, Bounds);

            Assert.AreEqual(-4.4f, nextX, 0.0001f);
        }

        [Test]
        public void TouchTarget_MovesTowardsTargetAndClampsAtRightBoundary()
        {
            var move = new PlayerMoveIntent(0f, 10f);

            var firstStep = PaddlePositionCalculator.CalculateNextX(0f, move, 8f, 0.02f, 2f, Bounds);
            var boundaryStep = PaddlePositionCalculator.CalculateNextX(4.35f, move, 8f, 0.02f, 2f, Bounds);

            Assert.AreEqual(0.16f, firstStep, 0.0001f);
            Assert.AreEqual(4.4f, boundaryStep, 0.0001f);
        }

        [Test]
        public void PaddleWiderThanField_ClampsCenterToFieldCenter()
        {
            var move = new PlayerMoveIntent(1f, null);
            var narrowBounds = new Rect(2f, -1f, 2f, 2f);

            var nextX = PaddlePositionCalculator.CalculateNextX(2f, move, 8f, 0.02f, 3f, narrowBounds);

            Assert.AreEqual(3f, nextX, 0.0001f);
        }
    }
}
