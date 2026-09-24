using Arkanoid.Input;
using UnityEngine;

namespace Arkanoid.Paddle
{
    public static class PaddlePositionCalculator
    {
        public static float CalculateNextX(
            float currentX,
            PlayerMoveIntent move,
            float speed,
            float deltaTime,
            float width,
            Rect bounds)
        {
            var halfWidth = width * 0.5f;
            var minX = bounds.xMin + halfWidth;
            var maxX = bounds.xMax - halfWidth;

            if (minX > maxX)
            {
                minX = bounds.center.x;
                maxX = minX;
            }

            float nextX;

            if (move.TargetWorldX.HasValue)
            {
                var targetX = Mathf.Clamp(move.TargetWorldX.Value, minX, maxX);
                nextX = Mathf.MoveTowards(currentX, targetX, speed * deltaTime);
            }
            else
            {
                var direction = Mathf.Clamp(move.Direction, -1f, 1f);
                nextX = currentX + direction * speed * deltaTime;
            }

            return Mathf.Clamp(nextX, minX, maxX);
        }
    }
}
