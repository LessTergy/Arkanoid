using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.UI
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _livesText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseButton;

        public event Action RestartRequested;
        public event Action PauseRequested;

        private void Awake()
        {
            _restartButton.onClick.AddListener(OnRestartClicked);
            _pauseButton.onClick.AddListener(OnPauseClicked);
        }

        private void OnDestroy()
        {
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _pauseButton.onClick.RemoveListener(OnPauseClicked);
        }

        public void SetLivesText(string value)
        {
            _livesText.text = value;
        }

        public void SetScoreText(string value)
        {
            _scoreText.text = value;
        }

        public void SetRestartEnabled(bool enabled)
        {
            _restartButton.interactable = enabled;
        }

        public void SetPauseEnabled(bool enabled)
        {
            _pauseButton.interactable = enabled;
        }

        private void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void OnPauseClicked()
        {
            PauseRequested?.Invoke();
        }
    }
}
