using System;
using Arkanoid.Ball;
using Arkanoid.Bonus;
using Arkanoid.Core.Bricks;
using TMPro;
using UnityEngine;

namespace Arkanoid.Bricks
{
    public sealed class BrickView : MonoBehaviour
    {
        [SerializeField] private BrickDefinition _definition;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _shieldRenderer;
        [SerializeField] private TMP_Text _stateLabel;

        public BrickState State { get; private set; }

        public BonusDropDefinition BonusDrop => _definition.BonusDrop;

        public event Action<BrickView> Destroyed;

        private BrickHitProcessor _hitProcessor;
        private BrickHitRequest _hitRequest;

        public void Initialize(BrickHitProcessor hitProcessor)
        {
            if (_hitRequest != null)
            {
                throw new InvalidOperationException("A BrickView can only be initialized once.");
            }

            var settings = _definition.CreateSettings();
            State = new BrickState(settings);
            _hitRequest = new BrickHitRequest(State);
            _hitProcessor = hitProcessor;
            _bodyRenderer.color = _definition.Color;
            RefreshStatePresentation();
        }

        public BrickHitResult Hit()
        {
            var result = _hitProcessor.Process(_hitRequest);
            if (result.Outcome != BrickHitOutcome.Ignored)
            {
                RefreshStatePresentation();
            }

            if (result.Outcome == BrickHitOutcome.Destroyed)
            {
                Destroyed?.Invoke(this);
                Destroy(gameObject);
            }

            return result;
        }

        private void RefreshStatePresentation()
        {
            var health = State.Settings.IsIndestructible ? "INF" : State.CurrentHealth.ToString();
            var shields = State.CurrentShieldCharges;
            _shieldRenderer.enabled = shields > 0;
            _stateLabel.text = shields > 0 ? $"HP {health} | S {shields}" : $"HP {health}";
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.TryGetComponent<BallController>(out _))
            {
                return;
            }

            Hit();
        }
    }
}
