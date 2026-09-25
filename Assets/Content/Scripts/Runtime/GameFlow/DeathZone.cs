using Arkanoid.Ball;
using Arkanoid.Core.GameFlow;
using Arkanoid.Paddle;
using UnityEngine;
using VContainer;

namespace Arkanoid.GameFlow
{
    public sealed class DeathZone : MonoBehaviour
    {
        private GameSession _gameSession;
        private LivesModel _livesModel;
        private BallController _ballController;
        private PaddleMovement _paddleMovement;

        [Inject]
        public void Construct(
            GameSession gameSession,
            LivesModel livesModel,
            BallController ballController,
            PaddleMovement paddleMovement)
        {
            _gameSession = gameSession;
            _livesModel = livesModel;
            _ballController = ballController;
            _paddleMovement = paddleMovement;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<BallController>(out var ball)
                || ball != _ballController
                || !_gameSession.TryLoseLife())
            {
                return;
            }

            _ballController.Lose();
            _livesModel.TryLoseLife();
            _paddleMovement.ResetPosition();

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
