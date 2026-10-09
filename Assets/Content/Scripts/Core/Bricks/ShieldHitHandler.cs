namespace Arkanoid.Core.Bricks
{
    public sealed class ShieldHitHandler : IBrickHitHandler
    {
        private readonly IBrickHitHandler _next;

        public ShieldHitHandler(IBrickHitHandler next)
        {
            _next = next;
        }

        public BrickHitResult Handle(BrickHitRequest request)
        {
            var state = request.State;
            if (state.TryConsumeShieldCharge())
            {
                var outcome = state.CurrentShieldCharges == 0
                    ? BrickHitOutcome.ShieldBroken
                    : BrickHitOutcome.ShieldAbsorbed;
                return new BrickHitResult(outcome, state);
            }

            return _next.Handle(request);
        }
    }
}
