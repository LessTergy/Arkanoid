using System;
using Arkanoid.Core.GameFlow;
using Arkanoid.Input;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.GameFlow
{
    internal sealed class GameplayPauseController : ITickable, IDisposable
    {
        private readonly InputSystemPlayerInput _playerInput;
        private readonly GameSession _gameSession;
        private float _timeScaleBeforePause;
        private int _lastToggleFrame = -1;

        public GameplayPauseController(InputSystemPlayerInput playerInput, GameSession gameSession)
        {
            _playerInput = playerInput;
            _gameSession = gameSession;
            _gameSession.StateChanged += OnGameSessionStateChanged;
        }

        public bool IsPaused { get; private set; }

        public event Action<bool> PauseChanged;

        public void Tick()
        {
            if (_playerInput.PausePressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_lastToggleFrame == Time.frameCount)
            {
                return;
            }

            if (IsPaused)
            {
                _lastToggleFrame = Time.frameCount;
                Resume();
                return;
            }

            if (_gameSession.State != GameSessionState.Ready
                && _gameSession.State != GameSessionState.Playing)
            {
                return;
            }

            _lastToggleFrame = Time.frameCount;
            _timeScaleBeforePause = Time.timeScale;
            IsPaused = true;
            _playerInput.SetGameplayInputEnabled(false);
            Time.timeScale = 0f;
            PauseChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            Time.timeScale = _timeScaleBeforePause;
            _playerInput.SetGameplayInputEnabled(true);
            IsPaused = false;
            PauseChanged?.Invoke(false);
        }

        public void Dispose()
        {
            _gameSession.StateChanged -= OnGameSessionStateChanged;
            PauseChanged = null;
            Resume();
        }

        private void OnGameSessionStateChanged(GameSessionState state)
        {
            if (state != GameSessionState.Ready && state != GameSessionState.Playing)
            {
                Resume();
            }
        }
    }
}
