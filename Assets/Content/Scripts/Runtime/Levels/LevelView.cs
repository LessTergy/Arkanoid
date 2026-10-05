using System;
using System.Collections.Generic;
using Arkanoid.Bricks;
using UnityEngine;

namespace Arkanoid.Levels
{
    public sealed class LevelView : MonoBehaviour
    {
        private readonly HashSet<BrickView> _remainingBricks = new();

        public int RemainingBricks => _remainingBricks.Count;
        public event Action<BrickView> BrickDestroyed;
        public event Action Finished;

        private void Awake()
        {
            var bricks = GetComponentsInChildren<BrickView>();
            foreach (var brick in bricks)
            {
                _remainingBricks.Add(brick);
                brick.Destroyed += OnBrickDestroyed;
            }

            if (_remainingBricks.Count == 0)
            {
                throw new InvalidOperationException("A level must contain at least one active BrickView.");
            }
        }

        private void OnBrickDestroyed(BrickView brick)
        {
            if (!_remainingBricks.Remove(brick))
            {
                return;
            }

            brick.Destroyed -= OnBrickDestroyed;
            BrickDestroyed?.Invoke(brick);

            if (_remainingBricks.Count == 0)
            {
                Finished?.Invoke();
            }
        }

        private void OnDestroy()
        {
            foreach (var brick in _remainingBricks)
            {
                brick.Destroyed -= OnBrickDestroyed;
            }
        }
    }
}
