namespace Arkanoid.Core.GameFlow
{
    internal abstract class GameSessionStateBase
    {
        public abstract GameSessionState Id { get; }

        public abstract GameSessionState? Handle(GameSessionSignal signal);
    }
}
