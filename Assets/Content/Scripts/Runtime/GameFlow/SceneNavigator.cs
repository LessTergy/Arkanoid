using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arkanoid.GameFlow
{
    public sealed class SceneNavigator : MonoBehaviour
    {
        [SerializeField] private SceneReference _bootstrapScene;
        [SerializeField] private SceneReference _gameplayScene;

        private bool _loadRequested;

        public void ToGameplay()
        {
            if (_loadRequested || SceneManager.GetActiveScene().path != _bootstrapScene.Path)
            {
                return;
            }

            _loadRequested = true;
            SceneManager.LoadSceneAsync(_gameplayScene.Path, LoadSceneMode.Single);
        }
    }
}
