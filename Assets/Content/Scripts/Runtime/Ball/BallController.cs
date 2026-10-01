using Arkanoid.Paddle;
using UnityEngine;
using VContainer;

namespace Arkanoid.Ball
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _attachedOffsetY = 0.5f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool _showDebugOverlay = true;
#endif

        private PaddleMovement _paddleMovement;
        private PaddleConfig _paddleConfig;
        private BallConfig _config;
        private Rigidbody2D _rigidbody;
        private BallAttachedState _attachedState;
        private BallFlyingState _flyingState;
        private BallLostState _lostState;
        private BallStoppedState _stoppedState;
        private BallStateBase _currentState;

        public BallState State => _currentState?.Id ?? BallState.Attached;
        internal Rigidbody2D Rigidbody => _rigidbody;

        [Inject]
        public void Construct(
            PaddleMovement paddleMovement,
            PaddleConfig paddleConfig,
            BallConfig config)
        {
            _paddleMovement = paddleMovement;
            _paddleConfig = paddleConfig;
            _config = config;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.simulated = false;
        }

        private void Start()
        {
            _flyingState = new BallFlyingState(this, _paddleMovement, _paddleConfig, _config);
            _attachedState = new BallAttachedState(this);
            _lostState = new BallLostState(this);
            _stoppedState = new BallStoppedState(this);
            ChangeState(_attachedState);
        }

        private void LateUpdate()
        {
            _currentState.LateUpdate();
        }

        private void FixedUpdate()
        {
            _currentState.FixedUpdate();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_showDebugOverlay)
            {
                return;
            }

            var velocity = _rigidbody.linearVelocity;
            var safeArea = Screen.safeArea;
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(safeArea.width / 100f), 14, 24);
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = fontSize
            };
            var text = $"Ball: {State}\nSpeed: {velocity.magnitude:F2} units/s";
            var width = Mathf.Min(safeArea.width - 24f, fontSize * 19f);
            var height = fontSize * 3.2f;
            var bounds = new Rect(
                safeArea.xMax - 12f - width,
                Screen.height - safeArea.yMin - 12f - height,
                width,
                height);
            GUI.Box(bounds, text, style);
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _currentState?.OnDrawGizmos();
        }
#endif

        private void OnCollisionEnter2D(Collision2D collision)
        {
            _currentState.OnCollisionEnter2D(collision);
        }

        public void Launch()
        {
            ChangeState(_flyingState);
        }

        public void Lose()
        {
            ChangeState(_lostState);
        }

        public void ResetToPaddle()
        {
            ChangeState(_attachedState);
        }

        public void PlaceAbovePaddle()
        {
            var paddlePosition = _paddleMovement.transform.position;
            var position = transform.position;
            transform.position = new Vector3(
                paddlePosition.x, paddlePosition.y + _attachedOffsetY, position.z);
        }

        public void StopMovement()
        {
            ChangeState(_stoppedState);
        }

        internal void LaunchPhysics(Vector2 direction, float speed)
        {
            var position = transform.position;
            _rigidbody.position = new Vector2(position.x, position.y);
            _rigidbody.simulated = true;
            _rigidbody.linearVelocity = direction * speed;
        }

        internal void StopPhysics()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
        }

        private void ChangeState(BallStateBase nextState)
        {
            _currentState = nextState;
            _currentState.Enter();
        }
    }
}
