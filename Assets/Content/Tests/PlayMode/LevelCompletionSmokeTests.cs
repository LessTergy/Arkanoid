using System.Collections;
using Arkanoid.Ball;
using Arkanoid.Bricks;
using Arkanoid.Core.GameFlow;
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
            var level = Object.FindFirstObjectByType<LevelView>();
            var ball = Object.FindFirstObjectByType<BallController>();
            Assert.IsNotNull(level);
            Assert.IsNotNull(ball);

            var bricks = level.GetComponentsInChildren<BrickView>();
            Assert.Greater(bricks.Length, 0, "The gameplay level must contain at least one brick.");
            Assert.AreEqual(bricks.Length, level.RemainingBricks);
            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.AreEqual(BallState.Attached, ball.State);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.AreEqual(GameSessionState.Playing, session.State);
            Assert.AreEqual(BallState.Flying, ball.State);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());

            var body = ball.GetComponent<Rigidbody2D>();
            var ballCollider = ball.GetComponent<Collider2D>();
            Assert.IsNotNull(ballCollider, "The ball needs a Collider2D.");
            Assert.IsTrue(body.simulated, "The launched ball must participate in 2D physics.");
            Assert.IsFalse(ballCollider.isTrigger, "The ball collider must produce collision callbacks.");
            var ballHalfHeight = ballCollider.bounds.extents.y;
            Assert.Greater(ballHalfHeight, 0f, "The ball collider must have nonzero bounds.");
            var brickColliders = new Collider2D[bricks.Length];
            for (var i = 0; i < bricks.Length; i++)
            {
                brickColliders[i] = bricks[i].GetComponent<Collider2D>();
                Assert.IsNotNull(brickColliders[i], $"Brick {bricks[i].name} needs a Collider2D.");
                Assert.IsFalse(brickColliders[i].isTrigger, $"Brick {bricks[i].name} must produce collision callbacks.");
            }

            for (var i = 0; i < bricks.Length; i++)
            {
                for (var j = 0; j < brickColliders.Length; j++)
                {
                    if (brickColliders[j] != null)
                    {
                        brickColliders[j].enabled = i == j;
                    }
                }

                var bounds = brickColliders[i].bounds;
                Assert.Greater(bounds.size.x, 0f, $"Brick {i + 1} collider has no bounds.");
                Assert.Greater(bounds.size.y, 0f, $"Brick {i + 1} collider has no bounds.");
                Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(ball.gameObject.layer, bricks[i].gameObject.layer),
                    $"Physics layers prevent collision with brick {i + 1}.");
                var remainingBeforeHit = level.RemainingBricks;
                var ballBoundsCenter = ballCollider.bounds.center;
                var colliderOffset = new Vector2(ballBoundsCenter.x - body.position.x, ballBoundsCenter.y - body.position.y);
                body.position = new Vector2(
                    bounds.center.x - colliderOffset.x,
                    bounds.min.y - ballHalfHeight - 0.1f - colliderOffset.y);
                body.linearVelocity = Vector2.up * 8f;

                var hitDeadline = Time.realtimeSinceStartup + 2f;
                while (level.RemainingBricks == remainingBeforeHit && Time.realtimeSinceStartup < hitDeadline)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.AreEqual(remainingBeforeHit - 1, level.RemainingBricks,
                    $"Brick {i + 1} did not register a ball collision within 2 seconds.");
            }

            Assert.AreEqual(0, level.RemainingBricks);
            Assert.AreEqual(GameSessionState.LevelComplete, session.State);
            Assert.AreEqual(BallState.Stopped, ball.State);
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
