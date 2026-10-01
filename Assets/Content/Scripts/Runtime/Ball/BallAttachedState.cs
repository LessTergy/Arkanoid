namespace Arkanoid.Ball
{
    internal sealed class BallAttachedState : BallStateBase
    {
        private readonly BallController _controller;

        public BallAttachedState(BallController controller)
        {
            _controller = controller;
        }

        public override BallState Id => BallState.Attached;

        public override void Enter()
        {
            _controller.StopPhysics();
            _controller.PlaceAbovePaddle();
        }

        public override void LateUpdate()
        {
            _controller.PlaceAbovePaddle();
        }
    }
}
