namespace Arkanoid.Core.GameFlow
{
    internal sealed class GamePlayingState : GameSessionStateBase
    {
        public override GameSessionState Id => GameSessionState.Playing;

        public override GameSessionState? Handle(GameSessionSignal signal)
        {
            if (signal == GameSessionSignal.LoseLife)
            {
                return GameSessionState.LifeLost;
            }

            if (signal == GameSessionSignal.CompleteLevel)
            {
                return GameSessionState.LevelComplete;
            }

            return null;
        }
    }
}
