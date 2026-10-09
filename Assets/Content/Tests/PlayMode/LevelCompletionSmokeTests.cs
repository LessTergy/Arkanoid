using System.Collections;
using System.Collections.Generic;
using Arkanoid.Ball;
using Arkanoid.Bricks;
using Arkanoid.Core.Bricks;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using Arkanoid.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class LevelCompletionSmokeTests
    {
        private Keyboard _keyboard;

        [UnityTest]
        public IEnumerator LaunchAndDestroyAllBricks_CompletesLevel()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Time.timeScale = 1f;

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            var navigator = Object.FindFirstObjectByType<SceneNavigator>();
            Assert.IsNotNull(navigator, "Bootstrap must contain a SceneNavigator.");
            navigator.ToGameplay();

            var loadDeadline = Time.realtimeSinceStartup + 10f;
            GameplayLifetimeScope scope = null;
            while (Time.realtimeSinceStartup < loadDeadline && scope == null)
            {
                scope = Object.FindFirstObjectByType<GameplayLifetimeScope>();
                yield return null;
            }

            Assert.IsNotNull(scope, "Gameplay was not loaded within 10 seconds.");
            var session = scope.Container.Resolve<GameSession>();
            var score = scope.Container.Resolve<ScoreService>();
            var combo = scope.Container.Resolve<ComboModel>();
            var level = Object.FindFirstObjectByType<LevelView>();
            var ball = Object.FindFirstObjectByType<BallController>();
            Assert.IsNotNull(level);
            Assert.IsNotNull(ball);

            var bricks = level.GetComponentsInChildren<BrickView>();
            Assert.Greater(bricks.Length, 0, "The gameplay level must contain at least one brick.");
            var probes = BrickHitTestSupport.Prepare(bricks);
            var orderedBricks = new List<BrickView>(bricks);
            orderedBricks.Sort((first, second) =>
                second.State.Settings.IsIndestructible.CompareTo(first.State.Settings.IsIndestructible));
            var destructibleCount = 0;
            foreach (var brick in bricks)
            {
                if (!brick.State.Settings.IsIndestructible)
                {
                    destructibleCount++;
                }
            }

            Assert.AreEqual(destructibleCount, level.RemainingBricks);
            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.AreEqual(BallState.Attached, ball.State);

            var expectedTotal = 0;
            var scoreEvents = 0;
            var destructions = 0;
            var finishedEvents = 0;
            var completedEvents = 0;
            var finishedTotal = -1;
            var completedTotal = -1;
            score.ScoreChanged += total =>
            {
                scoreEvents++;
                Assert.That(score.Total, Is.EqualTo(total));
                Assert.That(session.State, Is.EqualTo(GameSessionState.Playing),
                    "Each award, including the last brick, must precede LevelComplete.");
            };
            level.Finished += () =>
            {
                finishedEvents++;
                finishedTotal = score.Total;
                Assert.That(scoreEvents, Is.EqualTo(destructibleCount));
            };
            session.StateChanged += state =>
            {
                if (state == GameSessionState.LevelComplete)
                {
                    completedTotal = score.Total;
                    completedEvents++;
                }
            };

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.AreEqual(GameSessionState.Playing, session.State);
            Assert.AreEqual(BallState.Flying, ball.State);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());

            var body = ball.GetComponent<Rigidbody2D>();
            Assert.IsTrue(body.simulated, "The launched ball must participate in 2D physics.");
            foreach (var brick in orderedBricks)
            {
                var state = brick.State;
                if (state.Settings.IsIndestructible)
                {
                    yield return BrickHitTestSupport.Hit(ball, brick, probes[brick]);
                    Assert.That(state.CurrentHealth, Is.EqualTo(state.Settings.MaxHealth));
                    Assert.That(state.CurrentShieldCharges, Is.EqualTo(state.Settings.ShieldCharges));
                    Assert.That(level.RemainingBricks, Is.EqualTo(destructibleCount));
                    Assert.That(score.Total, Is.Zero);
                    Assert.That(combo.Count, Is.Zero);
                    Assert.That(scoreEvents, Is.Zero);
                    continue;
                }

                var hits = state.CurrentHealth + state.CurrentShieldCharges;
                for (var hit = 0; hit < hits; hit++)
                {
                    var health = state.CurrentHealth;
                    var shields = state.CurrentShieldCharges;
                    var remaining = level.RemainingBricks;
                    yield return BrickHitTestSupport.Hit(ball, brick, probes[brick]);
                    Assert.That(state.CurrentHealth, Is.EqualTo(shields > 0 ? health : health - 1));
                    Assert.That(state.CurrentShieldCharges, Is.EqualTo(shields > 0 ? shields - 1 : 0));
                    Assert.That(state.IsDestroyed, Is.EqualTo(hit == hits - 1));
                    if (!state.IsDestroyed)
                    {
                        Assert.That(level.RemainingBricks, Is.EqualTo(remaining));
                        Assert.That(score.Total, Is.EqualTo(expectedTotal));
                        Assert.That(combo.Count, Is.EqualTo(Mathf.Min(destructions, 5)));
                        Assert.That(scoreEvents, Is.EqualTo(destructions));
                        Assert.That(finishedEvents, Is.Zero);
                        Assert.That(completedEvents, Is.Zero);
                    }
                }

                destructions++;
                expectedTotal += state.Settings.BaseScore * Mathf.Min(destructions, 5);
                Assert.That(level.RemainingBricks, Is.EqualTo(destructibleCount - destructions));
                Assert.That(score.Total, Is.EqualTo(expectedTotal));
                Assert.That(combo.Count, Is.EqualTo(Mathf.Min(destructions, 5)));
                Assert.That(scoreEvents, Is.EqualTo(destructions), "Each destruction must award exactly once.");
            }

            Assert.AreEqual(0, level.RemainingBricks);
            Assert.AreEqual(GameSessionState.LevelComplete, session.State);
            Assert.AreEqual(BallState.Stopped, ball.State);
            Assert.That(finishedTotal, Is.EqualTo(expectedTotal));
            Assert.That(completedTotal, Is.EqualTo(expectedTotal));
            Assert.That(finishedEvents, Is.EqualTo(1));
            Assert.That(completedEvents, Is.EqualTo(1));
            foreach (var brick in level.GetComponentsInChildren<BrickView>())
            {
                Assert.That(brick.State.Settings.IsIndestructible, Is.True);
                Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Indestructible));
            }

            Assert.That(score.Total, Is.EqualTo(expectedTotal));
            Assert.That(scoreEvents, Is.EqualTo(destructibleCount));
            Assert.That(finishedEvents, Is.EqualTo(1));
            Assert.That(completedEvents, Is.EqualTo(1));

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.AreEqual(GameSessionState.LevelComplete, session.State);
            Assert.AreEqual(BallState.Stopped, ball.State);
            Assert.IsFalse(body.simulated);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                _keyboard = null;
            }

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }
    }
}
