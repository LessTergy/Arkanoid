using Arkanoid.Input;
using Arkanoid.Paddle;
using UnityEngine;
using VContainer;

namespace Arkanoid.Ball
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _attachedOffsetY = 0.5f;

        private Rigidbody2D _body;
        private IPlayerInput _playerInput;
        private PaddleMovement _paddleMovement;
        private BallConfig _config;

        public BallState State { get; private set; } = BallState.Attached;

        [Inject]
        public void Construct(IPlayerInput playerInput, PaddleMovement paddleMovement, BallConfig config)
        {
            _playerInput = playerInput;
            _paddleMovement = paddleMovement;
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
