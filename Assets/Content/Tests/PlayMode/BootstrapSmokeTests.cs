using System.Collections;
using System.Collections.Generic;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
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

            var scoreService = gameplayScope.Container.Resolve<ScoreService>();
            var combo = gameplayScope.Container.Resolve<ComboModel>();
            var doubleScore = gameplayScope.Container.Resolve<DoubleScoreDecorator>();
            Assert.That(scoreService.Total, Is.EqualTo(0));
            Assert.That(combo.Count, Is.EqualTo(0));
            Assert.That(doubleScore.IsEnabled, Is.False);
            Assert.That(gameplayScope.Container.Resolve<IScoreCalculator>(), Is.SameAs(doubleScore));

            combo.Advance();
            combo.Advance();
            combo.Advance();
            doubleScore.IsEnabled = true;
            AssertScoreSurvivesChildScope(gameplayScope, scoreService, combo, doubleScore, 600);
            AssertScoreSurvivesChildScope(gameplayScope, scoreService, combo, doubleScore, 1200);

            var appStartables = appScope.Container.Resolve<IReadOnlyList<IStartable>>();
            AssertHasEntryPoint(appStartables, "BootstrapEntryPoint");

            var gameplayStartables = gameplayScope.Container.Resolve<IReadOnlyList<IStartable>>();
            AssertHasEntryPoint(gameplayStartables, "GameplayPauseController");
            AssertHasEntryPoint(gameplayStartables, "LifeLossHandler");
            AssertHasEntryPoint(gameplayStartables, "GameplayScoreHandler");
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

            navigator.RestartGameplay();
            deadline = Time.realtimeSinceStartup + 10f;
            GameplayLifetimeScope restartedScope = null;

            while (Time.realtimeSinceStartup < deadline)
            {
                var candidate = Object.FindFirstObjectByType<GameplayLifetimeScope>();
                if (candidate != null && candidate != gameplayScope)
                {
                    restartedScope = candidate;
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(restartedScope, "Gameplay restart did not create a new scope within 10 seconds.");
            Assert.IsTrue(gameplayScope == null, "Previous gameplay scope survived full restart.");
            Assert.AreSame(appScope, restartedScope.Parent);
            var restartedScore = restartedScope.Container.Resolve<ScoreService>();
            var restartedCombo = restartedScope.Container.Resolve<ComboModel>();
            var restartedDouble = restartedScope.Container.Resolve<DoubleScoreDecorator>();
            Assert.That(restartedScore, Is.Not.SameAs(scoreService));
            Assert.That(restartedScore.Total, Is.EqualTo(0));
            Assert.That(restartedCombo, Is.Not.SameAs(combo));
            Assert.That(restartedCombo.Count, Is.EqualTo(0));
            Assert.That(restartedDouble, Is.Not.SameAs(doubleScore));
            Assert.That(restartedDouble.IsEnabled, Is.False);

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.IsTrue(restartedScope == null, "GameplayLifetimeScope survived scene unload.");
            Assert.AreSame(appScope, Object.FindFirstObjectByType<AppLifetimeScope>());
        }

        private static void AssertScoreSurvivesChildScope(
            GameplayLifetimeScope gameplayScope,
            ScoreService scoreService,
            ComboModel combo,
            DoubleScoreDecorator doubleScore,
            int expectedTotal)
        {
            var child = gameplayScope.CreateChild();
            try
            {
                var childScore = child.Container.Resolve<ScoreService>();
                Assert.That(childScore, Is.SameAs(scoreService));
                Assert.That(child.Container.Resolve<ComboModel>(), Is.SameAs(combo));
                Assert.That(child.Container.Resolve<DoubleScoreDecorator>(), Is.SameAs(doubleScore));
                Assert.That(child.Container.Resolve<IScoreCalculator>(), Is.SameAs(doubleScore));

                childScore.AddScore(100);
                Assert.That(childScore.Total, Is.EqualTo(expectedTotal));
            }
            finally
            {
                child.Dispose();
            }

            Assert.That(scoreService.Total, Is.EqualTo(expectedTotal));
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
