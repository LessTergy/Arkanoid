namespace Arkanoid.Core.Bricks
{
    public sealed class DamageHitHandler : IBrickHitHandler
    {
        public BrickHitResult Handle(BrickHitRequest request)
        {
            var state = request.State;
            if (!state.TryApplyDamage())
            {
                return new BrickHitResult(BrickHitOutcome.Ignored, state);
            }

            var outcome = state.IsDestroyed ? BrickHitOutcome.Destroyed : BrickHitOutcome.Damaged;
            return new BrickHitResult(outcome, state);
        }
    }
}
