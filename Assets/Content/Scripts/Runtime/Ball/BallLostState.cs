namespace Arkanoid.Ball
{
    internal sealed class BallLostState : BallStateBase
    {
        private readonly BallView _view;

        public BallLostState(BallView view)
        {
            _view = view;
        }

        public override BallState Id => BallState.Lost;

        public override void Enter()
        {
            _view.Stop();
        }
    }
}
