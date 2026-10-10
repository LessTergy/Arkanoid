using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.GameFlow;
using Arkanoid.Paddle;
using UnityEngine;
using VContainer;

namespace Arkanoid.Bonus
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class BonusPickup : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _fallSpeed = 3f;

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private BoxCollider2D _collider;
        private PaddleMovement _paddle;
        private DeathZone _deathZone;
        private GameSession _session;
        private GameplayPauseController _pause;
        private bool _removed;

        public event Action<BonusPickup> Collected;
        public event Action<BonusPickup> Removed;

        [Inject]
        internal void Construct(
            PaddleMovement paddle,
            DeathZone deathZone,
            GameSession session,
            GameplayPauseController pause)
        {
            ValidateConfiguration();
            _paddle = paddle;
            _deathZone = deathZone;
            _session = session;
            _pause = pause;
        }

        internal void ValidateConfiguration()
        {
            if (float.IsNaN(_fallSpeed) || float.IsInfinity(_fallSpeed) || _fallSpeed <= 0f)
            {
                throw new InvalidOperationException("BonusPickup fall speed must be finite and positive.");
            }

            if (_rigidbody == null || _collider == null
                || _rigidbody.gameObject != gameObject || _collider.gameObject != gameObject)
            {
                throw new InvalidOperationException(
                    "BonusPickup requires assigned Rigidbody2D and BoxCollider2D references on the same object.");
            }

            if (!enabled || _rigidbody.bodyType != RigidbodyType2D.Kinematic
                || !_rigidbody.simulated || (_rigidbody.constraints & RigidbodyConstraints2D.FreezePositionY) != 0
                || !_collider.enabled || !_collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "BonusPickup must be enabled, with a simulated Kinematic Rigidbody2D, "
                    + "unlocked Y position and an enabled trigger BoxCollider2D on the same object.");
            }
        }

        private void FixedUpdate()
        {
            if (!CanInteract())
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            _rigidbody.MovePosition(_rigidbody.position + Vector2.down * (_fallSpeed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider2D other)
        {
            if (_removed)
            {
                return;
            }

            if (other.gameObject == _deathZone.gameObject)
            {
                Remove();
                return;
            }

            if (other == _paddle.Collider && CanInteract())
            {
                Remove();
                Collected?.Invoke(this);
            }
        }

        private bool CanInteract()
        {
            return !_removed && _session.State == GameSessionState.Playing && !_pause.IsPaused;
        }

        public void Remove()
        {
            if (_removed)
            {
                return;
            }

            _removed = true;
            _collider.enabled = false;
            _rigidbody.simulated = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Removed?.Invoke(this);
        }
    }
}
