using System.Collections;
using Arkanoid.GameFlow;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_NavigatesToGameplayWithScopedServicesAndPackages()
        {
            var bootstrapPath = SceneUtility.GetScenePathByBuildIndex(0);
            Assert.IsNotEmpty(bootstrapPath, "Bootstrap must be the first enabled scene in Build Settings.");

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.AreEqual(bootstrapPath, SceneManager.GetActiveScene().path);

            var appScope = Object.FindFirstObjectByType<AppLifetimeScope>();
            var navigator = Object.FindFirstObjectByType<SceneNavigator>();
            Assert.IsNotNull(appScope, "AppLifetimeScope was not created.");
            Assert.IsNotNull(navigator, "SceneNavigator was not created by AppLifetimeScope.");

            navigator.ToGameplay();

            var deadline = Time.realtimeSinceStartup + 10f;
            GameplayLifetimeScope gameplayScope = null;

            while (Time.realtimeSinceStartup < deadline)
            {
                gameplayScope = Object.FindFirstObjectByType<GameplayLifetimeScope>();
                if (gameplayScope != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(gameplayScope, "Gameplay was not loaded within 10 seconds.");
            Assert.IsNotNull(appScope.Container);
            Assert.IsNotNull(gameplayScope.Container);
            Assert.AreSame(appScope, gameplayScope.Parent);
            Assert.AreEqual(gameplayScope.gameObject.scene, SceneManager.GetActiveScene());
            Assert.AreNotEqual(bootstrapPath, SceneManager.GetActiveScene().path);

            var addressablesHandle = Addressables.InitializeAsync(false);
            try
            {
                yield return addressablesHandle;
                Assert.AreEqual(AsyncOperationStatus.Succeeded, addressablesHandle.Status);
                Assert.IsNotNull(addressablesHandle.Result);
            }
            finally
            {
                Addressables.Release(addressablesHandle);
            }

            Assert.IsNotNull(DOTween.Init());

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.IsTrue(gameplayScope == null, "GameplayLifetimeScope survived scene unload.");
            Assert.AreSame(appScope, Object.FindFirstObjectByType<AppLifetimeScope>());
        }
    }
}
