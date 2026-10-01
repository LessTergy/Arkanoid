using Arkanoid.Paddle;

namespace Arkanoid.Ball
{
    internal sealed class BallAttachedState : BallStateBase
    {
        private readonly BallView _view;
        private readonly PaddleMovement _paddleMovement;

        public BallAttachedState(BallView view, PaddleMovement paddleMovement)
        {
            _view = view;
            _paddleMovement = paddleMovement;
        }

        public override BallState Id => BallState.Attached;

        public override void Enter()
        {
            _view.Stop();
            _view.HoldAbove(_paddleMovement.transform);
        }

        public override void LateUpdate()
        {
            _view.HoldAbove(_paddleMovement.transform);
        }
    }
}
