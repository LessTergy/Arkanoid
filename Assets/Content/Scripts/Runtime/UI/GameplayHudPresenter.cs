using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.GameFlow;

namespace Arkanoid.UI
{
    internal sealed class GameplayHudPresenter : IDisposable
    {
        private readonly GameplayHudView _view;
        private readonly LivesModel _livesModel;
        private readonly GameSession _gameSession;
        private readonly GameplayPauseController _pauseController;
        private readonly SceneNavigator _sceneNavigator;

        public GameplayHudPresenter(
            GameplayHudView view,
            LivesModel livesModel,
            GameSession gameSession,
            GameplayPauseController pauseController,
            SceneNavigator sceneNavigator)
        {
            _view = view;
            _livesModel = livesModel;
            _gameSession = gameSession;
            _pauseController = pauseController;
            _sceneNavigator = sceneNavigator;

            _livesModel.LivesChanged += OnLivesChanged;
            _gameSession.StateChanged += OnGameSessionStateChanged;
            _pauseController.PauseChanged += OnPauseChanged;
            _view.RestartRequested += OnRestartRequested;
            _view.PauseRequested += OnPauseRequested;

            OnLivesChanged(_livesModel.RemainingLives);
            OnGameSessionStateChanged(_gameSession.State);
            OnPauseChanged(_pauseController.IsPaused);
            _view.SetScoreText("Score: 0");
            _view.SetRestartEnabled(true);
        }

        public void Dispose()
        {
            _livesModel.LivesChanged -= OnLivesChanged;
            _gameSession.StateChanged -= OnGameSessionStateChanged;
            _pauseController.PauseChanged -= OnPauseChanged;
            _view.RestartRequested -= OnRestartRequested;
            _view.PauseRequested -= OnPauseRequested;
        }

        private void OnLivesChanged(int remainingLives)
        {
            _view.SetLivesText($"Lives: {remainingLives}");
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
            _view.SetPauseText(isPaused ? "Resume" : "Pause");
        }

        private void OnGameSessionStateChanged(GameSessionState state)
        {
            _view.SetPauseEnabled(state == GameSessionState.Ready || state == GameSessionState.Playing);
        }
    }
}
