using System;
using Arkanoid.Ball;
using UnityEngine;
using VContainer;

namespace Arkanoid.GameFlow
{
    public sealed class DeathZone : MonoBehaviour
    {
        private BallController _ballController;

        public event Action BallEntered;

        [Inject]
        public void Construct(BallController ballController)
        {
            _ballController = ballController;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<BallController>(out var ball)
                || ball != _ballController)
            {
                return;
            }

            BallEntered?.Invoke();
        }
    }
}
