using Arkanoid.Ball;
using Arkanoid.Core.GameFlow;
using Arkanoid.Input;
using VContainer.Unity;

namespace Arkanoid.GameFlow
{
    internal sealed class LaunchHandler : ITickable
    {
        private readonly IPlayerInput _playerInput;
        private readonly GameSession _gameSession;
        private readonly BallController _ballController;

        public LaunchHandler(IPlayerInput playerInput, GameSession gameSession, BallController ballController)
        {
            _playerInput = playerInput;
            _gameSession = gameSession;
            _ballController = ballController;
        }

        public void Tick()
        {
            if (_playerInput.LaunchPressedThisFrame && _gameSession.TryStartPlaying())
            {
                _ballController.Launch();
            }
        }
    }
}
