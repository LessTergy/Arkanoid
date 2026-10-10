using System;
using Arkanoid.Input;
using Arkanoid.Playfield;
using UnityEngine;
using VContainer;

namespace Arkanoid.Paddle
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class PaddleMovement : MonoBehaviour
    {
        private Rigidbody2D _body;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _boxCollider;
        private Vector2 _initialPosition;
        private IPlayerInput _playerInput;
        private PaddleConfig _config;
        private PlayfieldCamera _playfieldCamera;

        public float Width => _boxCollider.size.x * Mathf.Abs(transform.localScale.x);
        public BoxCollider2D Collider => _boxCollider;

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
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _boxCollider = GetComponent<BoxCollider2D>();
            _initialPosition = _body.position;

            var hasSlicedSprite = _spriteRenderer.sprite != null
                && _spriteRenderer.drawMode == SpriteDrawMode.Sliced;
            if (!hasSlicedSprite)
            {
                throw new InvalidOperationException("Paddle SpriteRenderer must have a sprite and use Sliced draw mode.");
            }
        }

        private void Start()
        {
            SetWidth(_config.Width);
        }

        public void SetWidth(float width)
        {
            var scaleX = Mathf.Abs(transform.localScale.x);
            var bounds = GetMovementBounds();
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f || width > bounds.width)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width,
                    "Paddle width must be finite, positive and fit inside the playfield.");
            }

            var localWidth = width / scaleX;
            if (scaleX == 0f || float.IsInfinity(scaleX) || float.IsNaN(localWidth) || float.IsInfinity(localWidth))
            {
                throw new InvalidOperationException("Paddle X scale must allow a finite local width.");
            }

            var spriteSize = _spriteRenderer.size;
            spriteSize.x = localWidth;
            _spriteRenderer.size = spriteSize;

            var colliderSize = _boxCollider.size;
            colliderSize.x = localWidth;
            _boxCollider.size = colliderSize;

            var position = _body.position;
            var clampedX = PaddlePositionCalculator.CalculateNextX(position.x, default, 0f, 0f, width, bounds);
            _body.position = new Vector2(clampedX, position.y);
            var worldPosition = transform.position;
            transform.position = new Vector3(clampedX, position.y, worldPosition.z);
        }

        public void ResetPosition()
        {
            _body.position = _initialPosition;
            var position = transform.position;
            transform.position = new Vector3(_initialPosition.x, _initialPosition.y, position.z);
        }

        private void FixedUpdate()
        {
            var position = _body.position;
            var move = _playerInput.Move;
            var nextX = PaddlePositionCalculator.CalculateNextX(
                position.x, move, _config.Speed, Time.fixedDeltaTime, Width, GetMovementBounds());

            if (!Mathf.Approximately(nextX, position.x))
            {
                _body.MovePosition(new Vector2(nextX, position.y));
            }
        }

        private Rect GetMovementBounds()
        {
            var bounds = _playfieldCamera.WorldBounds;
            var colliderCenterOffsetX = _boxCollider.offset.x * transform.localScale.x;
            return new Rect(bounds.xMin - colliderCenterOffsetX, bounds.yMin, bounds.width, bounds.height);
        }
    }
}
