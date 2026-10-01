using System;
using Arkanoid.Ball;
using Arkanoid.Core.GameFlow;
using Arkanoid.Levels;
using VContainer.Unity;

namespace Arkanoid.GameFlow
{
    internal sealed class LevelFinishedHandler : IStartable, IDisposable
    {
        private readonly LevelView _levelView;
        private readonly BallController _ballController;
        private readonly GameSession _gameSession;

        public LevelFinishedHandler(LevelView levelView, BallController ballController, GameSession gameSession)
        {
            _levelView = levelView;
            _ballController = ballController;
            _gameSession = gameSession;
        }

        public void Start()
        {
            _levelView.Finished += OnLevelFinished;
        }

        public void Dispose()
        {
            _levelView.Finished -= OnLevelFinished;
        }

        private void OnLevelFinished()
        {
            if (_gameSession.State != GameSessionState.Playing)
            {
                return;
            }

            _ballController.StopMovement();
            _gameSession.CompleteLevel();
        }
    }
}
