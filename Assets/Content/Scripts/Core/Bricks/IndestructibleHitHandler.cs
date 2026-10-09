namespace Arkanoid.Core.Bricks
{
    public sealed class IndestructibleHitHandler : IBrickHitHandler
    {
        private readonly IBrickHitHandler _next;

        public IndestructibleHitHandler(IBrickHitHandler next)
        {
            _next = next;
        }

        public BrickHitResult Handle(BrickHitRequest request)
        {
            if (request.State.Settings.IsIndestructible)
            {
                return new BrickHitResult(BrickHitOutcome.Indestructible, request.State);
            }

            return _next.Handle(request);
        }
    }
}
