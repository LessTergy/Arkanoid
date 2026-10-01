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

            if (_spriteRenderer.sprite == null || _spriteRenderer.drawMode != SpriteDrawMode.Sliced)
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
            if (width <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            var localWidth = width / scaleX;
            var spriteSize = _spriteRenderer.size;
            spriteSize.x = localWidth;
            _spriteRenderer.size = spriteSize;

            var colliderSize = _boxCollider.size;
            colliderSize.x = localWidth;
            _boxCollider.size = colliderSize;
        }

        public void ResetPosition()
        {
            _body.position = _initialPosition;
            var position = transform.position;
            transform.position = new Vector3(_initialPosition.x, _initialPosition.y, position.z);
        }

        private void FixedUpdate()
        {
            var bounds = _playfieldCamera.WorldBounds;
            var colliderCenterOffsetX = _boxCollider.offset.x * transform.localScale.x;
            var originBounds = new Rect(
                bounds.xMin - colliderCenterOffsetX, bounds.yMin, bounds.width, bounds.height);
            var position = _body.position;
            var move = _playerInput.Move;
            var nextX = PaddlePositionCalculator.CalculateNextX(
                position.x, move, _config.Speed, Time.fixedDeltaTime, Width, originBounds);

            if (!Mathf.Approximately(nextX, position.x))
            {
                _body.MovePosition(new Vector2(nextX, position.y));
            }
        }
    }
}
