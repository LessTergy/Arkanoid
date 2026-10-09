namespace Arkanoid.Core.Bricks
{
    public interface IBrickHitHandler
    {
        BrickHitResult Handle(BrickHitRequest request);
    }
}
