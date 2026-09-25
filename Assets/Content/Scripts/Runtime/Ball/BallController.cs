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

        private IPlayerInput _playerInput;
        private PaddleMovement _paddleMovement;
        private PaddleConfig _paddleConfig;
        private BallConfig _config;
        private BallView _view;
        private BallAttachedState _attachedState;
        private BallFlyingState _flyingState;
        private BallLostState _lostState;
        private BallStateBase _currentState;

        public BallState State => _currentState?.Id ?? BallState.Attached;

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
            _view = new BallView(GetComponent<Rigidbody2D>(), transform, () => _attachedOffsetY);
        }

        private void Start()
        {
            _flyingState = new BallFlyingState(_view, _paddleMovement, _paddleConfig, _config);
            _attachedState = new BallAttachedState(_view, _playerInput, _paddleMovement, _flyingState);
            _lostState = new BallLostState(_view);
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
            var velocity = _view.Body.linearVelocity;
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

        private void ChangeState(BallStateBase nextState)
        {
            _currentState = nextState;
            _currentState.Enter();
        }
    }
}
