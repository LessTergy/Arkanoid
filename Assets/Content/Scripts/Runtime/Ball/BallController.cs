using Arkanoid.Input;
using Arkanoid.Paddle;
using UnityEngine;
using VContainer;

namespace Arkanoid.Ball
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallController : MonoBehaviour
    {
        private const float MinimumVelocitySqrMagnitude = 0.000001f;

        [SerializeField, Min(0f)] private float _attachedOffsetY = 0.5f;

        private Rigidbody2D _body;
        private IPlayerInput _playerInput;
        private PaddleMovement _paddleMovement;
        private PaddleConfig _paddleConfig;
        private BallConfig _config;
        private Vector2 _lastFlyingDirection;

        public BallState State { get; private set; } = BallState.Attached;

        [Inject]
        public void Construct(
            IPlayerInput playerInput,
            PaddleMovement paddleMovement,
            PaddleConfig paddleConfig,
            BallConfig config)
        {
            _playerInput = playerInput;
            _paddleMovement = paddleMovement;
            _paddleConfig = paddleConfig;
            _config = config;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.simulated = false;
        }

        private void Update()
        {
            if (State == BallState.Attached && _playerInput.LaunchPressedThisFrame)
            {
                TryEnterFlying();
            }
        }

        private void LateUpdate()
        {
            if (State == BallState.Attached)
            {
                HoldAbovePaddle();
            }
        }

        private void FixedUpdate()
        {
            if (State != BallState.Flying)
            {
                return;
            }

            var velocity = _body.linearVelocity;
            if (velocity.sqrMagnitude > MinimumVelocitySqrMagnitude)
            {
                _lastFlyingDirection = velocity.normalized;
            }

            _body.linearVelocity = _lastFlyingDirection * _config.Speed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (State != BallState.Flying
                || collision.gameObject != _paddleMovement.gameObject
                || _body.position.y <= _paddleMovement.transform.position.y)
            {
                return;
            }

            var contactX = collision.GetContact(0).point.x;
            var paddleCenterX = _paddleMovement.transform.position.x;
            // Override the solver's reflection so the hit position controls the bounce direction.
            var direction = BallBounceCalculator.CalculatePaddleBounceDirection(
                contactX, paddleCenterX, _paddleConfig.Width, _config.MinimumVerticalComponent);

            _lastFlyingDirection = direction;
            _body.linearVelocity = direction * _config.Speed;
        }

        public bool TryEnterFlying()
        {
            if (State != BallState.Attached)
            {
                return false;
            }

            HoldAbovePaddle();
            var minimumAngle = Mathf.Min(_config.MinimumLaunchAngleDegrees, _config.MaximumLaunchAngleDegrees);
            var maximumAngle = Mathf.Max(_config.MinimumLaunchAngleDegrees, _config.MaximumLaunchAngleDegrees);
            var angle = Random.Range(minimumAngle, maximumAngle) * Mathf.Deg2Rad;
            var horizontalSign = Random.value < 0.5f ? -1f : 1f;
            var direction = new Vector2(horizontalSign * Mathf.Sin(angle), Mathf.Cos(angle)).normalized;

            _lastFlyingDirection = direction;
            var launchPosition = transform.position;
            _body.position = new Vector2(launchPosition.x, launchPosition.y);
            _body.simulated = true;
            _body.linearVelocity = direction * _config.Speed;
            State = BallState.Flying;
            return true;
        }

        public bool TryEnterLost()
        {
            if (State != BallState.Flying)
            {
                return false;
            }

            _body.linearVelocity = Vector2.zero;
            _body.simulated = false;
            State = BallState.Lost;
            return true;
        }

        public void EnterAttached()
        {
            _body.linearVelocity = Vector2.zero;
            _body.simulated = false;
            State = BallState.Attached;
            HoldAbovePaddle();
        }

        private void HoldAbovePaddle()
        {
            var paddlePosition = _paddleMovement.transform.position;
            var position = transform.position;
            transform.position = new Vector3(paddlePosition.x, paddlePosition.y + _attachedOffsetY, position.z);
        }
    }
}
