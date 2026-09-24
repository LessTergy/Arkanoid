namespace Arkanoid.Input
{
    public readonly struct PlayerMoveIntent
    {
        public float Direction { get; }
        public float? TargetWorldX { get; }

        public PlayerMoveIntent(float direction, float? targetWorldX)
        {
            Direction = direction;
            TargetWorldX = targetWorldX;
        }
    }
}
