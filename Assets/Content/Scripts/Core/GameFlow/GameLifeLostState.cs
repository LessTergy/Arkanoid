namespace Arkanoid.Core.GameFlow
{
    internal sealed class GameLifeLostState : GameSessionStateBase
    {
        public override GameSessionState Id => GameSessionState.LifeLost;

        public override GameSessionState? Handle(GameSessionSignal signal)
        {
            if (signal == GameSessionSignal.ResumeAfterLifeLoss)
            {
                return GameSessionState.Ready;
            }

            if (signal == GameSessionSignal.EndGame)
            {
                return GameSessionState.GameOver;
            }

            return null;
        }
    }
}
