namespace Arkanoid.Core.Bricks
{
    public sealed class BrickHitRequest
    {
        public BrickHitRequest(BrickState state)
        {
            State = state;
        }

        public BrickState State { get; }
    }
}
