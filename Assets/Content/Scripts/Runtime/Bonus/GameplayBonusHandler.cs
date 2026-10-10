using System;
using System.Collections.Generic;
using Arkanoid.Bricks;
using Arkanoid.Core.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.GameFlow;
using Arkanoid.Levels;
using VContainer.Unity;

namespace Arkanoid.Bonus
{
    internal sealed class GameplayBonusHandler : IStartable, IDisposable
    {
        private readonly LevelView _level;
        private readonly GameSession _session;
        private readonly GameplayPauseController _pause;
        private readonly BonusDropService _dropService;
        private readonly BonusFactory _factory;
        private readonly ExpandPaddleEffect _expandPaddle;
        private readonly HashSet<BonusPickup> _pickups = new();

        public GameplayBonusHandler(
            LevelView level,
            GameSession session,
            GameplayPauseController pause,
            BonusDropService dropService,
            BonusFactory factory,
            ExpandPaddleEffect expandPaddle)
        {
            _level = level;
            _session = session;
            _pause = pause;
            _dropService = dropService;
            _factory = factory;
            _expandPaddle = expandPaddle;
        }

        public void Start()
        {
            _level.BrickDestroyed += OnBrickDestroyed;
            _session.StateChanged += OnSessionStateChanged;
        }

        public void Dispose()
        {
            _level.BrickDestroyed -= OnBrickDestroyed;
            _session.StateChanged -= OnSessionStateChanged;
            ClearPickups();
        }

        private void ClearPickups()
        {
            foreach (var pickup in _pickups)
            {
                if (pickup != null)
                {
                    pickup.Collected -= OnPickupCollected;
                    pickup.Removed -= OnPickupRemoved;
                    pickup.Remove();
                }
            }

            _pickups.Clear();
        }

        private void OnSessionStateChanged(GameSessionState state)
        {
            if (state == GameSessionState.LifeLost || state == GameSessionState.LevelComplete
                || state == GameSessionState.GameOver)
            {
                ClearPickups();
                _expandPaddle.Reset();
            }
        }

        private void OnBrickDestroyed(BrickView brick)
        {
            if (_session.State != GameSessionState.Playing || _pause.IsPaused || _level.RemainingBricks == 0)
            {
                return;
            }

            var profile = brick.BonusDrop;
            if (profile == null || !_dropService.ShouldDrop(profile.CreateSettings()))
            {
                return;
            }

            var pickup = _factory.Create(profile.PickupPrefab, brick.transform.position);
            _pickups.Add(pickup);
            pickup.Collected += OnPickupCollected;
            pickup.Removed += OnPickupRemoved;
        }

        private void OnPickupCollected(BonusPickup pickup)
        {
            _expandPaddle.Apply();
        }

        private void OnPickupRemoved(BonusPickup pickup)
        {
            pickup.Collected -= OnPickupCollected;
            pickup.Removed -= OnPickupRemoved;
            _pickups.Remove(pickup);
        }
    }
}
