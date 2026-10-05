using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using VContainer.Unity;

namespace Arkanoid.UI
{
    internal sealed class GameplayHudPresenter : IStartable, IDisposable
    {
        private readonly GameplayHudView _view;
        private readonly LivesModel _livesModel;
        private readonly ScoreService _scoreService;
        private readonly ComboModel _comboModel;
        private readonly GameSession _gameSession;
        private readonly GameplayPauseController _pauseController;
        private readonly SceneNavigator _sceneNavigator;

        public GameplayHudPresenter(
            GameplayHudView view,
            LivesModel livesModel,
            ScoreService scoreService,
            ComboModel comboModel,
            GameSession gameSession,
            GameplayPauseController pauseController,
            SceneNavigator sceneNavigator)
        {
            _view = view;
            _livesModel = livesModel;
            _scoreService = scoreService;
            _comboModel = comboModel;
            _gameSession = gameSession;
            _pauseController = pauseController;
            _sceneNavigator = sceneNavigator;
        }

        public void Start()
        {
            _livesModel.LivesChanged += OnLivesChanged;
            _scoreService.ScoreChanged += OnScoreChanged;
            _comboModel.ComboChanged += OnComboChanged;
            _gameSession.StateChanged += OnGameSessionStateChanged;
            _pauseController.PauseChanged += OnPauseChanged;
            _view.RestartRequested += OnRestartRequested;
            _view.PauseRequested += OnPauseRequested;

            OnLivesChanged(_livesModel.RemainingLives);
            OnGameSessionStateChanged(_gameSession.State);
            OnPauseChanged(_pauseController.IsPaused);
            OnScoreChanged(_scoreService.Total);
            OnComboChanged(_comboModel.Count);
            _view.SetRestartEnabled(true);
        }

        public void Dispose()
        {
            _livesModel.LivesChanged -= OnLivesChanged;
            _scoreService.ScoreChanged -= OnScoreChanged;
            _comboModel.ComboChanged -= OnComboChanged;
            _gameSession.StateChanged -= OnGameSessionStateChanged;
            _pauseController.PauseChanged -= OnPauseChanged;
            _view.RestartRequested -= OnRestartRequested;
            _view.PauseRequested -= OnPauseRequested;
        }

        private void OnLivesChanged(int remainingLives)
        {
            _view.SetLivesCount(remainingLives);
        }

        private void OnScoreChanged(int total)
        {
            _view.SetScore(total);
        }

        private void OnComboChanged(int count)
        {
            _view.SetCombo(count);
        }

        private void OnRestartRequested()
        {
            _view.SetRestartEnabled(false);
            _pauseController.Resume();
            _sceneNavigator.RestartGameplay();
        }

        private void OnPauseRequested()
        {
            _pauseController.TogglePause();
        }

        private void OnPauseChanged(bool isPaused)
        {
            _view.SetPaused(isPaused);
        }

        private void OnGameSessionStateChanged(GameSessionState state)
        {
            _view.SetPauseEnabled(state == GameSessionState.Ready || state == GameSessionState.Playing);
        }
    }
}
