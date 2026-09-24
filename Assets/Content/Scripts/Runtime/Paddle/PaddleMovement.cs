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
        }

        private void FixedUpdate()
        {
            var bounds = _playfieldCamera.WorldBounds;
            var halfWidth = _config.Width * 0.5f;
            var minX = bounds.xMin + halfWidth;
            var maxX = bounds.xMax - halfWidth;
            var position = _body.position;

            if (minX > maxX)
            {
                minX = bounds.center.x;
                maxX = minX;
            }

            var move = _playerInput.Move;
            float nextX;

            if (move.TargetWorldX.HasValue)
            {
                var targetX = Mathf.Clamp(move.TargetWorldX.Value, minX, maxX);
                nextX = Mathf.MoveTowards(position.x, targetX, _config.Speed * Time.fixedDeltaTime);
            }
            else
            {
                var direction = Mathf.Clamp(move.Direction, -1f, 1f);
                nextX = position.x + direction * _config.Speed * Time.fixedDeltaTime;
            }

            nextX = Mathf.Clamp(nextX, minX, maxX);

            if (!Mathf.Approximately(nextX, position.x))
            {
                _body.MovePosition(new Vector2(nextX, position.y));
            }
        }
    }
}
