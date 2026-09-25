namespace Arkanoid.Core.GameFlow
{
    internal enum GameSessionSignal
    {
        StartPlaying = 0,
        LoseLife = 1,
        ResumeAfterLifeLoss = 2,
        EndGame = 3,
        CompleteLevel = 4,
        Restart = 5
    }
}
