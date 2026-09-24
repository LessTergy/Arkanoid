namespace Arkanoid.Input
{
    public interface IPlayerInput
    {
        PlayerMoveIntent Move { get; }
        bool LaunchPressedThisFrame { get; }
        bool PausePressedThisFrame { get; }
    }
}
