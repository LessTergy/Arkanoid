namespace Arkanoid.Core.Bricks
{
    public sealed class BrickHitProcessor
    {
        private readonly IBrickHitHandler _head;

        public BrickHitProcessor(IBrickHitHandler head)
        {
            _head = head;
        }

        public BrickHitResult Process(BrickHitRequest request)
        {
            if (request.State.IsDestroyed)
            {
                return new BrickHitResult(BrickHitOutcome.Ignored, request.State);
            }

            return _head.Handle(request);
        }
    }
}
