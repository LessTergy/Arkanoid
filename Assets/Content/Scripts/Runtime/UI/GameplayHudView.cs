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
        [SerializeField] private TMP_Text _pauseButtonText;

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

        public void SetRestartEnabled(bool value)
        {
            _restartButton.interactable = value;
        }

        public void SetPauseEnabled(bool value)
        {
            _pauseButton.interactable = value;
        }

        public void SetPauseText(string value)
        {
            _pauseButtonText.text = value;
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
