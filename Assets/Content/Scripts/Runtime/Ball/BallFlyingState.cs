using Arkanoid.Paddle;
using UnityEngine;

namespace Arkanoid.Ball
{
    internal sealed class BallFlyingState : BallStateBase
    {
        private const float MinimumVelocitySqrMagnitude = 0.000001f;

        private readonly BallController _controller;
        private readonly PaddleMovement _paddleMovement;
        private readonly PaddleConfig _paddleConfig;
        private readonly BallConfig _config;
        private Vector2 _lastFlyingDirection;

        public BallFlyingState(
            BallController controller,
            PaddleMovement paddleMovement,
            PaddleConfig paddleConfig,
            BallConfig config)
        {
            _controller = controller;
            _paddleMovement = paddleMovement;
            _paddleConfig = paddleConfig;
            _config = config;
        }

        public override BallState Id => BallState.Flying;

        public override void Enter()
        {
            _controller.PlaceAbovePaddle();
            var minimumAngle = Mathf.Min(_config.MinimumLaunchAngleDegrees, _config.MaximumLaunchAngleDegrees);
            var maximumAngle = Mathf.Max(_config.MinimumLaunchAngleDegrees, _config.MaximumLaunchAngleDegrees);
            var angle = Random.Range(minimumAngle, maximumAngle);
            var horizontalSign = Random.value < 0.5f ? -1f : 1f;
            _lastFlyingDirection = BallBounceCalculator.CalculateLaunchDirection(
                angle, horizontalSign,
                _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            _controller.LaunchPhysics(_lastFlyingDirection, _config.Speed);
        }

        public override void FixedUpdate()
        {
            var velocity = _controller.Rigidbody.linearVelocity;
            if (velocity.sqrMagnitude > MinimumVelocitySqrMagnitude)
            {
                _lastFlyingDirection = BallBounceCalculator.LimitDirection(
                    velocity, _lastFlyingDirection,
                    _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            }

            _controller.Rigidbody.linearVelocity = _lastFlyingDirection * _config.Speed;
        }

        public override void OnCollisionEnter2D(Collision2D collision)
        {
            var shouldSkipPaddleBounce = collision.gameObject != _paddleMovement.gameObject
                || _controller.Rigidbody.position.y <= _paddleMovement.transform.position.y;
            if (shouldSkipPaddleBounce)
            {
                return;
            }

            var contactX = collision.GetContact(0).point.x;
            var paddleCenterX = _paddleMovement.transform.position.x;
            // Override the solver's reflection so the hit position controls the bounce direction.
            var direction = BallBounceCalculator.CalculatePaddleBounceDirection(
                contactX, paddleCenterX, _paddleConfig.Width, _config.MinimumVerticalComponent);

            _lastFlyingDirection = BallBounceCalculator.LimitDirection(
                direction, _lastFlyingDirection,
                _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            _controller.Rigidbody.linearVelocity = _lastFlyingDirection * _config.Speed;
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            var direction = _controller.Rigidbody.linearVelocity.normalized;
            var start = _controller.transform.position;
            var end = start + (Vector3)(direction * 2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.08f);
        }
#endif
    }
}
