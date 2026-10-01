namespace Arkanoid.Ball
{
    internal sealed class BallLostState : BallStateBase
    {
        private readonly BallController _controller;

        public BallLostState(BallController controller)
        {
            _controller = controller;
        }

        public override BallState Id => BallState.Lost;

        public override void Enter()
        {
            _controller.StopPhysics();
        }
    }
}
