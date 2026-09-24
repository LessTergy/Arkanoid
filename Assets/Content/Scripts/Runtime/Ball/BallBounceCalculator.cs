using UnityEngine;

namespace Arkanoid.Ball
{
    public static class BallBounceCalculator
    {
        public static Vector2 CalculateLaunchDirection(
            float angleDegrees,
            float horizontalSign,
            float minimumHorizontalComponent,
            float minimumVerticalComponent)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            var direction = new Vector2(horizontalSign * Mathf.Sin(angle), Mathf.Cos(angle));
            return LimitDirection(
                direction, new Vector2(horizontalSign, 1f),
                minimumHorizontalComponent, minimumVerticalComponent);
        }

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

        public static Vector2 LimitDirection(
            Vector2 direction,
            Vector2 previousDirection,
            float minimumHorizontalComponent,
            float minimumVerticalComponent)
        {
            var normalized = direction.normalized;
            var minimumVertical = Mathf.Clamp01(minimumVerticalComponent);
            var maximumHorizontal = Mathf.Sqrt(1f - minimumVertical * minimumVertical);
            var minimumHorizontal = Mathf.Min(Mathf.Clamp01(minimumHorizontalComponent), maximumHorizontal);
            var horizontalMagnitude = Mathf.Clamp(Mathf.Abs(normalized.x), minimumHorizontal, maximumHorizontal);
            var verticalMagnitude = Mathf.Sqrt(1f - horizontalMagnitude * horizontalMagnitude);
            var horizontalSign = normalized.x != 0f ? Mathf.Sign(normalized.x) : Mathf.Sign(previousDirection.x);
            var verticalSign = normalized.y != 0f ? Mathf.Sign(normalized.y) : Mathf.Sign(previousDirection.y);

            return new Vector2(horizontalSign * horizontalMagnitude, verticalSign * verticalMagnitude);
        }
    }
}
