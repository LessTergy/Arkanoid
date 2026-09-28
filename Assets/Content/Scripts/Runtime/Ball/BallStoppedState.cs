namespace Arkanoid.Ball
{
    internal sealed class BallStoppedState : BallStateBase
    {
        private readonly BallView _view;

        public BallStoppedState(BallView view)
        {
            _view = view;
        }

        public override BallState Id => BallState.Stopped;

        public override void Enter()
        {
            _view.Stop();
        }
    }
}
