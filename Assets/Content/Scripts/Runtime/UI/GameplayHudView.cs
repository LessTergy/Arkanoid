using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.UI
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text _livesText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _pauseButtonText;
        [SerializeField] private string _livesFormat = "Lives: {0}";
        [SerializeField] private string _scoreFormat = "Score: {0}";
        [SerializeField] private string _pauseLabel = "Pause";
        [SerializeField] private string _resumeLabel = "Resume";

        [Header("Buttons")]
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

        public void SetLivesCount(int remainingLives)
        {
            _livesText.text = string.Format(_livesFormat, remainingLives);
        }

        public void SetScore(int score)
        {
            _scoreText.text = string.Format(_scoreFormat, score);
        }

        public void SetRestartEnabled(bool value)
        {
            _restartButton.interactable = value;
        }

        public void SetPauseEnabled(bool value)
        {
            _pauseButton.interactable = value;
        }

        public void SetPaused(bool isPaused)
        {
            _pauseButtonText.text = isPaused ? _resumeLabel : _pauseLabel;
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
