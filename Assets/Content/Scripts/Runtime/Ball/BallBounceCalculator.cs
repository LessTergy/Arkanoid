using UnityEngine;

namespace Arkanoid.Ball
{
    public static class BallBounceCalculator
    {
        public static Vector2 CalculatePaddleBounceDirection(
            float contactX,
            float paddleCenterX,
            float paddleWidth,
            float minimumVerticalComponent)
        {
            var normalizedContact = Mathf.Clamp((contactX - paddleCenterX) / (paddleWidth * 0.5f), -1f, 1f);
            var verticalComponent = Mathf.Clamp01(minimumVerticalComponent);
            var maximumHorizontalComponent = Mathf.Sqrt(1f - verticalComponent * verticalComponent);
            var horizontalComponent = normalizedContact * maximumHorizontalComponent;
            var upwardComponent = Mathf.Sqrt(1f - horizontalComponent * horizontalComponent);

            return new Vector2(horizontalComponent, upwardComponent).normalized;
        }
    }
}
