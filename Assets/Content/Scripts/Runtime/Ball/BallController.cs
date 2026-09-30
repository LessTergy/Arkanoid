using Arkanoid.Core.GameFlow;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool _showDebugOverlay = true;
#endif

        private IPlayerInput _playerInput;
        private GameSession _gameSession;
        private PaddleMovement _paddleMovement;
        private PaddleConfig _paddleConfig;
        private BallConfig _config;
        private BallView _view;
        private BallAttachedState _attachedState;
        private BallFlyingState _flyingState;
        private BallLostState _lostState;
        private BallStoppedState _stoppedState;
        private BallStateBase _currentState;

        public BallState State => _currentState?.Id ?? BallState.Attached;

        [Inject]
        public void Construct(
            IPlayerInput playerInput,
            GameSession gameSession,
            PaddleMovement paddleMovement,
            PaddleConfig paddleConfig,
            BallConfig config)
        {
            _playerInput = playerInput;
            _gameSession = gameSession;
            _paddleMovement = paddleMovement;
            _paddleConfig = paddleConfig;
            _config = config;
        }

        private void Awake()
        {
            _view = new BallView(GetComponent<Rigidbody2D>(), transform, () => _attachedOffsetY);
        }

        private void Start()
        {
            _flyingState = new BallFlyingState(_view, _paddleMovement, _paddleConfig, _config);
            _attachedState = new BallAttachedState(_view, _playerInput, _gameSession, _paddleMovement, _flyingState);
            _lostState = new BallLostState(_view);
            _stoppedState = new BallStoppedState(_view);
            ChangeState(_attachedState);
        }

        private void Update()
        {
            var nextState = _currentState.Update();
            if (nextState != null)
            {
                ChangeState(nextState);
            }
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

            var velocity = _view.Body.linearVelocity;
            var safeArea = Screen.safeArea;
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(safeArea.width / 100f), 14, 24);
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = fontSize
            };
            var text = $"Session: {_gameSession.State}\nBall: {State}\nSpeed: {velocity.magnitude:F2} units/s";
            var width = Mathf.Min(safeArea.width - 24f, fontSize * 19f);
            var height = fontSize * 4.2f;
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
            _view.HoldAbove(_paddleMovement.transform);
        }

        public void StopMovement()
        {
            ChangeState(_stoppedState);
        }

        private void ChangeState(BallStateBase nextState)
        {
            _currentState = nextState;
            _currentState.Enter();
        }
    }
}
