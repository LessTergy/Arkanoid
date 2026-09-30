using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.GameFlow;

namespace Arkanoid.UI
{
    internal sealed class GameplayHudPresenter : IDisposable
    {
        private readonly GameplayHudView _view;
        private readonly LivesModel _livesModel;
        private readonly SceneNavigator _sceneNavigator;

        public GameplayHudPresenter(
            GameplayHudView view,
            LivesModel livesModel,
            SceneNavigator sceneNavigator)
        {
            _view = view;
            _livesModel = livesModel;
            _sceneNavigator = sceneNavigator;

            _livesModel.LivesChanged += OnLivesChanged;
            _view.RestartRequested += OnRestartRequested;

            OnLivesChanged(_livesModel.RemainingLives);
            _view.SetScoreText("Score: 0");
            _view.SetRestartEnabled(true);
            _view.SetPauseEnabled(false);
        }

        public void Dispose()
        {
            _livesModel.LivesChanged -= OnLivesChanged;
            _view.RestartRequested -= OnRestartRequested;
        }

        private void OnLivesChanged(int remainingLives)
        {
            _view.SetLivesText($"Lives: {remainingLives}");
        }

        private void OnRestartRequested()
        {
            _view.SetRestartEnabled(false);
            _sceneNavigator.RestartGameplay();
        }
    }
}
