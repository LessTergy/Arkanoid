using System;
using Arkanoid.Bricks;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.Levels;
using VContainer.Unity;

namespace Arkanoid.GameFlow
{
    internal sealed class GameplayScoreHandler : IStartable, IDisposable
    {
        private const int BasicBaseScore = 100;

        private readonly LevelView _levelView;
        private readonly GameSession _gameSession;
        private readonly ComboModel _comboModel;
        private readonly ScoreService _scoreService;

        public GameplayScoreHandler(
            LevelView levelView,
            GameSession gameSession,
            ComboModel comboModel,
            ScoreService scoreService)
        {
            _levelView = levelView;
            _gameSession = gameSession;
            _comboModel = comboModel;
            _scoreService = scoreService;
        }

        public void Start()
        {
            _levelView.BrickDestroyed += OnBrickDestroyed;
            _gameSession.StateChanged += OnGameSessionStateChanged;
        }

        public void Dispose()
        {
            _levelView.BrickDestroyed -= OnBrickDestroyed;
            _gameSession.StateChanged -= OnGameSessionStateChanged;
        }

        private void OnBrickDestroyed(BrickView brick)
        {
            if (_gameSession.State != GameSessionState.Playing)
            {
                return;
            }

            if (brick.TypeId != BrickTypeId.Basic)
            {
                throw new ArgumentOutOfRangeException(nameof(brick), brick.TypeId, "Unsupported brick type.");
            }

            _comboModel.Advance();
            _scoreService.AddScore(BasicBaseScore);
        }

        private void OnGameSessionStateChanged(GameSessionState state)
        {
            if (state == GameSessionState.LifeLost)
            {
                _comboModel.Reset();
            }
        }
    }
}
