using Arkanoid.Core.GameFlow;
using Arkanoid.Input;
using Arkanoid.Paddle;

namespace Arkanoid.Ball
{
    internal sealed class BallAttachedState : BallStateBase
    {
        private readonly BallView _view;
        private readonly IPlayerInput _playerInput;
        private readonly GameSession _gameSession;
        private readonly PaddleMovement _paddleMovement;
        private readonly BallFlyingState _flyingState;

        public BallAttachedState(
            BallView view,
            IPlayerInput playerInput,
            GameSession gameSession,
            PaddleMovement paddleMovement,
            BallFlyingState flyingState)
        {
            _view = view;
            _playerInput = playerInput;
            _gameSession = gameSession;
            _paddleMovement = paddleMovement;
            _flyingState = flyingState;
        }

        public override BallState Id => BallState.Attached;

        public override void Enter()
        {
            _view.Stop();
            _view.HoldAbove(_paddleMovement.transform);
        }

        public override BallStateBase Update()
        {
            if (_playerInput.LaunchPressedThisFrame && _gameSession.TryStartPlaying())
            {
                return _flyingState;
            }

            return null;
        }

        public override void LateUpdate()
        {
            _view.HoldAbove(_paddleMovement.transform);
        }
    }
}
