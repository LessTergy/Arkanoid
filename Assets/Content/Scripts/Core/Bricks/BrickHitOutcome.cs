namespace Arkanoid.Core.Bricks
{
    public enum BrickHitOutcome
    {
        Ignored = 0,
        Indestructible,
        ShieldAbsorbed,
        ShieldBroken,
        Damaged,
        Destroyed,
    }
}
