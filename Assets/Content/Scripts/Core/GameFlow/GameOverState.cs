namespace Arkanoid.Core.GameFlow
{
    internal sealed class GameOverState : GameSessionStateBase
    {
        public override GameSessionState Id => GameSessionState.GameOver;

        public override GameSessionState? Handle(GameSessionSignal signal)
        {
            if (signal == GameSessionSignal.Restart)
            {
                return GameSessionState.Ready;
            }

            return null;
        }
    }
}
