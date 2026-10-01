namespace Arkanoid.Ball
{
    internal sealed class BallStoppedState : BallStateBase
    {
        private readonly BallController _controller;

        public BallStoppedState(BallController controller)
        {
            _controller = controller;
        }

        public override BallState Id => BallState.Stopped;

        public override void Enter()
        {
            _controller.StopPhysics();
        }
    }
}
