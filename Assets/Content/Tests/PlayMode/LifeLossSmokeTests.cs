using System.Collections;
using System.Collections.Generic;
using Arkanoid.Ball;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using Arkanoid.Paddle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class LifeLossSmokeTests
    {
        private const int MaxTriggerPhysicsSteps = 5;

        private Keyboard _keyboard;

        [UnityTest]
        public IEnumerator ThreeDeathZoneEntries_ConsumeLivesAndEndGame()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Time.timeScale = 1f;

            var bootstrapPath = SceneUtility.GetScenePathByBuildIndex(0);
            Assert.IsNotEmpty(bootstrapPath, "Bootstrap must be the first enabled scene in Build Settings.");
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
            var lives = scope.Container.Resolve<LivesModel>();
            var score = scope.Container.Resolve<ScoreService>();
            var combo = scope.Container.Resolve<ComboModel>();
            score.AddScore(100);
            var ball = Object.FindFirstObjectByType<BallController>();
            var paddle = Object.FindFirstObjectByType<PaddleMovement>();
            var deathZone = Object.FindFirstObjectByType<DeathZone>();
            Assert.IsNotNull(ball);
            Assert.IsNotNull(paddle);
            Assert.IsNotNull(deathZone);

            var ballBody = ball.GetComponent<Rigidbody2D>();
            var ballCollider = ball.GetComponent<Collider2D>();
            var paddleBody = paddle.GetComponent<Rigidbody2D>();
            var zoneCollider = deathZone.GetComponent<Collider2D>();
            Assert.IsNotNull(ballCollider, "The ball needs a Collider2D.");
            Assert.IsNotNull(zoneCollider, "DeathZone needs a Collider2D.");
            Assert.IsTrue(zoneCollider.isTrigger, "DeathZone collider must be a trigger.");
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(ball.gameObject.layer, deathZone.gameObject.layer),
                "Physics layers prevent the ball from entering DeathZone.");

            var initialPaddlePosition = paddleBody.position;
            var stateChanges = new List<GameSessionState>();
            var lifeChanges = new List<int>();
            var livesAtLifeLost = new List<int>();
            var totalBeforeLifeLost = score.Total;
            session.StateChanged += state =>
            {
                stateChanges.Add(state);
                if (state == GameSessionState.LifeLost)
                {
                    livesAtLifeLost.Add(lives.RemainingLives);
                    Assert.That(combo.Count, Is.EqualTo(0), "Combo must reset on LifeLost, including the last life.");
                    Assert.That(score.Total, Is.EqualTo(totalBeforeLifeLost), "LifeLost must preserve the accumulated total.");
                }
            };
            lives.LivesChanged += lifeChanges.Add;
            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.AreEqual(LivesModel.InitialLives, lives.RemainingLives);
            Assert.AreEqual(BallState.Attached, ball.State);

            for (var hit = 1; hit <= LivesModel.InitialLives; hit++)
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
                yield return null;
                Assert.AreEqual(GameSessionState.Playing, session.State);
                Assert.AreEqual(BallState.Flying, ball.State);
                Assert.IsTrue(ballBody.simulated);

                InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                yield return null;

                if (hit == 1)
                {
                    var stateChangeCount = stateChanges.Count;
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
                    yield return null;
                    Assert.AreEqual(stateChangeCount, stateChanges.Count,
                        "Pressing Launch during flight must not start the session again.");
                    Assert.AreEqual(BallState.Flying, ball.State);
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    yield return null;
                }

                yield return new WaitForFixedUpdate();
                yield return null;

                Assert.AreEqual(LivesModel.InitialLives - hit + 1, lives.RemainingLives,
                    $"Life {hit} was lost before the directed DeathZone entry.");

                paddleBody.position = initialPaddlePosition + Vector2.right;
                combo.Reset();
                combo.Advance();
                combo.Advance();
                Assert.That(combo.Count, Is.EqualTo(2));
                totalBeforeLifeLost = score.Total;
                var zoneBounds = zoneCollider.bounds;
                Assert.Greater(zoneBounds.size.x, 0f, "DeathZone collider has no bounds.");
                Assert.Greater(zoneBounds.size.y, 0f, "DeathZone collider has no bounds.");
                var colliderOffset = (Vector2)ball.transform.TransformVector(ballCollider.offset);
                ballBody.position = (Vector2)zoneBounds.center - colliderOffset;
                ballBody.linearVelocity = Vector2.down * 8f;

                for (var step = 0;
                     step < MaxTriggerPhysicsSteps && lives.RemainingLives == LivesModel.InitialLives - hit + 1;
                     step++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.AreEqual(LivesModel.InitialLives - hit, lives.RemainingLives,
                    $"DeathZone did not consume exactly one life on entry {hit} within {MaxTriggerPhysicsSteps} physics steps. " +
                    $"Ball position: {ballBody.position}, velocity: {ballBody.linearVelocity}, " +
                    $"simulated: {ballBody.simulated}, session: {session.State}.");
                Assert.AreEqual(initialPaddlePosition.x, paddleBody.position.x, 0.001f);
                Assert.AreEqual(initialPaddlePosition.y, paddleBody.position.y, 0.001f);
                Assert.IsFalse(ballBody.simulated);
                Assert.That(combo.Count, Is.EqualTo(0));
                Assert.That(score.Total, Is.EqualTo(totalBeforeLifeLost));

                if (hit < LivesModel.InitialLives)
                {
                    Assert.AreEqual(GameSessionState.Ready, session.State);
                    Assert.AreEqual(BallState.Attached, ball.State);
                }
                else
                {
                    Assert.AreEqual(GameSessionState.GameOver, session.State);
                    Assert.AreEqual(BallState.Lost, ball.State);
                }

                Assert.AreEqual(initialPaddlePosition.x, ball.transform.position.x, 0.001f);
                Assert.Greater(ball.transform.position.y, paddle.transform.position.y);
            }

            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, lifeChanges);
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, livesAtLifeLost);
            CollectionAssert.AreEqual(
                new[]
                {
                    GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.Ready,
                    GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.Ready,
                    GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.GameOver
                },
                stateChanges);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.AreEqual(GameSessionState.GameOver, session.State);
            Assert.AreEqual(0, lives.RemainingLives);
            Assert.AreEqual(BallState.Lost, ball.State);
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
