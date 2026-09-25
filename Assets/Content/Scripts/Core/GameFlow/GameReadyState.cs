namespace Arkanoid.Core.GameFlow
{
    internal sealed class GameReadyState : GameSessionStateBase
    {
        public override GameSessionState Id => GameSessionState.Ready;

        public override GameSessionState? Handle(GameSessionSignal signal)
        {
            if (signal == GameSessionSignal.StartPlaying)
            {
                return GameSessionState.Playing;
            }

            return null;
        }
    }
}
