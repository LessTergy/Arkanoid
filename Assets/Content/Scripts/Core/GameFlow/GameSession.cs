using System;
using System.Collections.Generic;

namespace Arkanoid.Core.GameFlow
{
    public sealed class GameSession
    {
        private readonly Dictionary<GameSessionState, GameSessionStateBase> _states =
            new()
            {
                { GameSessionState.Ready, new GameReadyState() },
                { GameSessionState.Playing, new GamePlayingState() },
                { GameSessionState.LifeLost, new GameLifeLostState() },
                { GameSessionState.LevelComplete, new GameLevelCompleteState() },
                { GameSessionState.GameOver, new GameOverState() }
            };

        private GameSessionStateBase _currentState;

        public GameSession()
        {
            _currentState = _states[GameSessionState.Ready];
        }

        public GameSessionState State => _currentState.Id;

        public event Action<GameSessionState> StateChanged;

        public bool TryStartPlaying()
        {
            return Handle(GameSessionSignal.StartPlaying);
        }

        public bool TryLoseLife()
        {
            return Handle(GameSessionSignal.LoseLife);
        }

        public void ResumeAfterLifeLoss()
        {
            Handle(GameSessionSignal.ResumeAfterLifeLoss);
        }

        public void EndGame()
        {
            Handle(GameSessionSignal.EndGame);
        }

        public void CompleteLevel()
        {
            Handle(GameSessionSignal.CompleteLevel);
        }

        public void Restart()
        {
            Handle(GameSessionSignal.Restart);
        }

        private bool Handle(GameSessionSignal signal)
        {
            var nextState = _currentState.Handle(signal);
            if (!nextState.HasValue)
            {
                return false;
            }

            _currentState = _states[nextState.Value];
            StateChanged?.Invoke(_currentState.Id);
            return true;
        }
    }
}
