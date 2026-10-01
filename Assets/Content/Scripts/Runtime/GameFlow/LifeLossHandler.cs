using System;
using Arkanoid.Ball;
using Arkanoid.Core.GameFlow;
using Arkanoid.Paddle;
using VContainer.Unity;

namespace Arkanoid.GameFlow
{
    internal sealed class LifeLossHandler : IStartable, IDisposable
    {
        private readonly DeathZone _deathZone;
        private readonly LivesModel _livesModel;
        private readonly BallController _ballController;
        private readonly PaddleMovement _paddleMovement;
        private readonly GameSession _gameSession;

        public LifeLossHandler(
            DeathZone deathZone,
            LivesModel livesModel,
            BallController ballController,
            PaddleMovement paddleMovement,
            GameSession gameSession)
        {
            _deathZone = deathZone;
            _livesModel = livesModel;
            _ballController = ballController;
            _paddleMovement = paddleMovement;
            _gameSession = gameSession;
        }

        public void Start()
        {
            _deathZone.BallEntered += OnBallEntered;
        }

        public void Dispose()
        {
            _deathZone.BallEntered -= OnBallEntered;
        }

        private void OnBallEntered()
        {
            if (_gameSession.State != GameSessionState.Playing || !_livesModel.TryLoseLife())
            {
                return;
            }

            _ballController.Lose();
            _paddleMovement.ResetPosition();
            _gameSession.TryLoseLife();

            if (_livesModel.RemainingLives > 0)
            {
                _ballController.ResetToPaddle();
                _gameSession.ResumeAfterLifeLoss();
            }
            else
            {
                _ballController.PlaceAbovePaddle();
                _gameSession.EndGame();
            }
        }
    }
}
