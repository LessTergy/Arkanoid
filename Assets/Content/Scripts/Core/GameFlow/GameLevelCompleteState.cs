namespace Arkanoid.Core.GameFlow
{
    internal sealed class GameLevelCompleteState : GameSessionStateBase
    {
        public override GameSessionState Id => GameSessionState.LevelComplete;

        public override GameSessionState? Handle(GameSessionSignal signal)
        {
            return null;
        }
    }
}
