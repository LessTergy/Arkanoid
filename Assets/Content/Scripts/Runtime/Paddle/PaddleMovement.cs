using Arkanoid.Input;
using Arkanoid.Playfield;
using UnityEngine;
using VContainer;

namespace Arkanoid.Paddle
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PaddleMovement : MonoBehaviour
    {
        private Rigidbody2D _body;
        private Vector2 _initialPosition;
        private IPlayerInput _playerInput;
        private PaddleConfig _config;
        private PlayfieldCamera _playfieldCamera;

        [Inject]
        public void Construct(IPlayerInput playerInput, PaddleConfig config, PlayfieldCamera playfieldCamera)
        {
            _playerInput = playerInput;
            _config = config;
            _playfieldCamera = playfieldCamera;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _initialPosition = _body.position;
        }

        public void ResetPosition()
        {
            _body.position = _initialPosition;
        }

        private void FixedUpdate()
        {
            var bounds = _playfieldCamera.WorldBounds;
            var position = _body.position;
            var move = _playerInput.Move;
            var nextX = PaddlePositionCalculator.CalculateNextX(
                position.x, move, _config.Speed, Time.fixedDeltaTime, _config.Width, bounds);

            if (!Mathf.Approximately(nextX, position.x))
            {
                _body.MovePosition(new Vector2(nextX, position.y));
            }
        }
    }
}
