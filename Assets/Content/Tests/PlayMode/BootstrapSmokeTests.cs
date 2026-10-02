using System.Collections;
using System.Collections.Generic;
using Arkanoid.Core.GameFlow;
using Arkanoid.GameFlow;
using Arkanoid.Input;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

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

            Assert.AreSame(navigator, gameplayScope.Container.Resolve<SceneNavigator>());
            Assert.IsNotNull(gameplayScope.Container.Resolve<GameSession>());
            Assert.IsNotNull(gameplayScope.Container.Resolve<LivesModel>());
            Assert.IsNotNull(gameplayScope.Container.Resolve<IPlayerInput>());

            var appStartables = appScope.Container.Resolve<IReadOnlyList<IStartable>>();
            AssertHasEntryPoint(appStartables, "BootstrapEntryPoint");

            var gameplayStartables = gameplayScope.Container.Resolve<IReadOnlyList<IStartable>>();
            AssertHasEntryPoint(gameplayStartables, "GameplayPauseController");
            AssertHasEntryPoint(gameplayStartables, "LifeLossHandler");
            AssertHasEntryPoint(gameplayStartables, "LevelFinishedHandler");
            AssertHasEntryPoint(gameplayStartables, "GameplayHudPresenter");

            var gameplayTickables = gameplayScope.Container.Resolve<IReadOnlyList<ITickable>>();
            AssertHasEntryPoint(gameplayTickables, "GameplayPauseController");
            AssertHasEntryPoint(gameplayTickables, "LaunchHandler");

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

        private static void AssertHasEntryPoint<T>(IReadOnlyList<T> entryPoints, string typeName)
        {
            foreach (var entryPoint in entryPoints)
            {
                if (entryPoint.GetType().Name == typeName)
                {
                    return;
                }
            }

            Assert.Fail($"{typeName} was not resolved as {typeof(T).Name}.");
        }
    }
}
