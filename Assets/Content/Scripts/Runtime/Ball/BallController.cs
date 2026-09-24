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
                _lastFlyingDirection = BallBounceCalculator.LimitDirection(
                    velocity, _lastFlyingDirection,
                    _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            }

            _body.linearVelocity = _lastFlyingDirection * _config.Speed;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            var velocity = _body.linearVelocity;
            var direction = velocity.normalized;
            var safeArea = Screen.safeArea;
            var fontSize = Mathf.Max(14, Mathf.RoundToInt(safeArea.width / 50f));
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = fontSize
            };
            var text = $"Ball: {State}\nDirection: ({direction.x:F2}, {direction.y:F2})\nSpeed: {velocity.magnitude:F2} units/s";
            var bounds = new Rect(
                safeArea.xMin + 12f,
                Screen.height - safeArea.yMax + 12f,
                Mathf.Min(safeArea.width - 24f, fontSize * 27f),
                fontSize * 4.4f);
            GUI.Box(bounds, text, style);
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || State != BallState.Flying)
            {
                return;
            }

            var direction = _body.linearVelocity.normalized;
            var start = transform.position;
            var end = start + (Vector3)(direction * 2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.08f);
        }
#endif

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

            _lastFlyingDirection = BallBounceCalculator.LimitDirection(
                direction, _lastFlyingDirection,
                _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            _body.linearVelocity = _lastFlyingDirection * _config.Speed;
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
            var angle = Random.Range(minimumAngle, maximumAngle);
            var horizontalSign = Random.value < 0.5f ? -1f : 1f;
            _lastFlyingDirection = BallBounceCalculator.CalculateLaunchDirection(
                angle, horizontalSign,
                _config.MinimumHorizontalComponent, _config.MinimumVerticalComponent);
            var launchPosition = transform.position;
            _body.position = new Vector2(launchPosition.x, launchPosition.y);
            _body.simulated = true;
            _body.linearVelocity = _lastFlyingDirection * _config.Speed;
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
