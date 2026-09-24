using Arkanoid.Ball;
using NUnit.Framework;
using UnityEngine;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BallBounceCalculatorTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void LaunchDirection_UsesAngleAndChosenSide()
        {
            var right = BallBounceCalculator.CalculateLaunchDirection(30f, 1f, 0.2f, 0.5f);
            var left = BallBounceCalculator.CalculateLaunchDirection(30f, -1f, 0.2f, 0.5f);

            Assert.AreEqual(0.5f, right.x, Tolerance);
            Assert.AreEqual(-0.5f, left.x, Tolerance);
            Assert.AreEqual(0.8660254f, right.y, Tolerance);
            Assert.AreEqual(right.y, left.y, Tolerance);
            Assert.AreEqual(1f, right.magnitude, Tolerance);
            Assert.AreEqual(1f, left.magnitude, Tolerance);
        }

        [Test]
        public void LaunchDirection_ZeroAngleKeepsChosenSideAfterLimiting()
        {
            var direction = BallBounceCalculator.CalculateLaunchDirection(0f, -1f, 0.2f, 0.5f);

            Assert.AreEqual(-0.2f, direction.x, Tolerance);
            Assert.Greater(direction.y, 0f);
            Assert.AreEqual(1f, direction.magnitude, Tolerance);
        }

        [Test]
        public void PaddleBounce_CenterPointsUp()
        {
            var direction = BallBounceCalculator.CalculatePaddleBounceDirection(2f, 2f, 2f, 0.5f);

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(1f, direction.y, Tolerance);
        }

        [Test]
        public void PaddleBounce_EdgesTiltSymmetricallyAndRespectVerticalMinimum()
        {
            var left = BallBounceCalculator.CalculatePaddleBounceDirection(-1f, 0f, 2f, 0.5f);
            var right = BallBounceCalculator.CalculatePaddleBounceDirection(1f, 0f, 2f, 0.5f);

            Assert.AreEqual(-0.8660254f, left.x, Tolerance);
            Assert.AreEqual(0.8660254f, right.x, Tolerance);
            Assert.AreEqual(0.5f, left.y, Tolerance);
            Assert.AreEqual(0.5f, right.y, Tolerance);
            Assert.AreEqual(1f, left.magnitude, Tolerance);
            Assert.AreEqual(1f, right.magnitude, Tolerance);
        }

        [Test]
        public void PaddleBounce_ContactBeyondEdgeIsClampedToEdge()
        {
            var edge = BallBounceCalculator.CalculatePaddleBounceDirection(1f, 0f, 2f, 0.5f);
            var beyondEdge = BallBounceCalculator.CalculatePaddleBounceDirection(3f, 0f, 2f, 0.5f);

            Assert.AreEqual(edge.x, beyondEdge.x, Tolerance);
            Assert.AreEqual(edge.y, beyondEdge.y, Tolerance);
        }

        [Test]
        public void PaddleBounce_CenterDirectionIsLimitedAwayFromVertical()
        {
            var bounce = BallBounceCalculator.CalculatePaddleBounceDirection(0f, 0f, 2f, 0.5f);
            var direction = BallBounceCalculator.LimitDirection(bounce, new Vector2(-0.5f, 0.8660254f), 0.2f, 0.5f);

            Assert.AreEqual(-0.2f, direction.x, Tolerance);
            Assert.GreaterOrEqual(direction.y, 0.5f);
            Assert.AreEqual(1f, direction.magnitude, Tolerance);
        }
    }
}
