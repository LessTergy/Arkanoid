using System.Collections;
using System.Collections.Generic;
using Arkanoid.Ball;
using Arkanoid.Bricks;
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
    public sealed class GameplayScoreIntegrationTests
    {
        private readonly List<int> _publishedTotals = new();
        private Keyboard _keyboard;
        private GameSession _session;
        private LivesModel _lives;
        private ScoreService _score;
        private ComboModel _combo;
        private DoubleScoreDecorator _doubleScore;
        private LevelView _level;
        private BallController _ball;
        private Rigidbody2D _ballBody;
        private Collider2D _ballCollider;
        private Collider2D _zoneCollider;
        private Collider2D[] _brickColliders;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            _publishedTotals.Clear();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

            var navigator = Object.FindFirstObjectByType<SceneNavigator>();
            Assert.That(navigator, Is.Not.Null);
            navigator.ToGameplay();

            var deadline = Time.realtimeSinceStartup + 10f;
            GameplayLifetimeScope scope = null;
            while (scope == null && Time.realtimeSinceStartup < deadline)
            {
                scope = Object.FindFirstObjectByType<GameplayLifetimeScope>();
                yield return null;
            }

            Assert.That(scope, Is.Not.Null, "Gameplay was not loaded within 10 seconds.");
            _session = scope.Container.Resolve<GameSession>();
            _lives = scope.Container.Resolve<LivesModel>();
            _score = scope.Container.Resolve<ScoreService>();
            _combo = scope.Container.Resolve<ComboModel>();
            _doubleScore = scope.Container.Resolve<DoubleScoreDecorator>();
            _level = scope.Container.Resolve<LevelView>();
            _ball = scope.Container.Resolve<BallController>();
            _ballBody = _ball.GetComponent<Rigidbody2D>();
            _ballCollider = _ball.GetComponent<Collider2D>();
            _zoneCollider = scope.Container.Resolve<DeathZone>().GetComponent<Collider2D>();

            var bricks = _level.GetComponentsInChildren<BrickView>();
            Assert.That(bricks.Length, Is.GreaterThanOrEqualTo(3),
                "The score integration scenarios require at least three active Basic bricks.");
            _brickColliders = new Collider2D[bricks.Length];
            for (var i = 0; i < bricks.Length; i++)
            {
                Assert.That(bricks[i].TypeId, Is.EqualTo(BrickTypeId.Basic));
                _brickColliders[i] = bricks[i].GetComponent<Collider2D>();
                Assert.That(_brickColliders[i], Is.Not.Null);
                _brickColliders[i].enabled = false;
            }

            Assert.That(_session.State, Is.EqualTo(GameSessionState.Ready));
            Assert.That(_score.Total, Is.EqualTo(0));
            Assert.That(_combo.Count, Is.EqualTo(0));
            Assert.That(_doubleScore.IsEnabled, Is.False);
            _score.ScoreChanged += OnScoreChanged;
        }

        [UnityTest]
        public IEnumerator LifeLost_AfterRealDestructions_PreservesTotalAndRestartsCombo()
        {
            yield return Launch();
            yield return DestroyBrick(0);
            yield return DestroyBrick(1);
            Assert.That(_score.Total, Is.EqualTo(300));
            Assert.That(_combo.Count, Is.EqualTo(2));

            var lifeLostObserved = false;
            _session.StateChanged += state =>
            {
                if (state == GameSessionState.LifeLost)
                {
                    lifeLostObserved = true;
                    Assert.That(_combo.Count, Is.EqualTo(0));
                    Assert.That(_score.Total, Is.EqualTo(300));
                }
            };

            yield return EnterDeathZone();

            Assert.That(lifeLostObserved, Is.True);
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Ready));
            Assert.That(_ball.State, Is.EqualTo(BallState.Attached));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 300 }),
                "Losing a life must not award points or publish a score change.");

            yield return Launch();
            yield return DestroyBrick(2);

            Assert.That(_score.Total, Is.EqualTo(400));
            Assert.That(_combo.Count, Is.EqualTo(1));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 300, 400 }));
        }

        [UnityTest]
        public IEnumerator NegativeInput_InGameplayComposition_PreservesStateAndNextDestructionUsesNextCombo()
        {
            yield return Launch();
            yield return DestroyBrick(0);
            yield return DestroyBrick(1);
            _doubleScore.IsEnabled = true;
            var remainingBeforeRejection = _level.RemainingBricks;

            var exception = Assert.Throws<System.ArgumentOutOfRangeException>(() => _score.AddScore(-100));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(_score.Total, Is.EqualTo(300));
            Assert.That(_combo.Count, Is.EqualTo(2));
            Assert.That(_doubleScore.IsEnabled, Is.True);
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_level.RemainingBricks, Is.EqualTo(remainingBeforeRejection));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 300 }));

            yield return DestroyBrick(2);

            Assert.That(_score.Total, Is.EqualTo(900));
            Assert.That(_combo.Count, Is.EqualTo(3));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 300, 900 }));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_score != null)
            {
                _score.ScoreChanged -= OnScoreChanged;
            }

            Time.timeScale = 1f;
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                _keyboard = null;
            }

            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }

        private IEnumerator Launch()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_ball.State, Is.EqualTo(BallState.Flying));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        }

        private IEnumerator DestroyBrick(int index)
        {
            _brickColliders[index].enabled = true;
            var bounds = _brickColliders[index].bounds;
            Assert.That(bounds.size.y, Is.GreaterThan(0f));
            var remainingBeforeHit = _level.RemainingBricks;
            var colliderOffset = (Vector2)_ballCollider.bounds.center - _ballBody.position;
            _ballBody.position = new Vector2(
                bounds.center.x - colliderOffset.x,
                bounds.min.y - _ballCollider.bounds.extents.y - 0.1f - colliderOffset.y);
            _ballBody.linearVelocity = Vector2.up * 8f;

            var deadline = Time.realtimeSinceStartup + 2f;
            while (_level.RemainingBricks == remainingBeforeHit && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_level.RemainingBricks, Is.EqualTo(remainingBeforeHit - 1),
                $"Brick {index + 1} did not register a ball collision within 2 seconds.");
            yield return null;
        }

        private IEnumerator EnterDeathZone()
        {
            var livesBeforeEntry = _lives.RemainingLives;
            var bounds = _zoneCollider.bounds;
            Assert.That(_zoneCollider.isTrigger, Is.True);
            Assert.That(bounds.size.y, Is.GreaterThan(0f));
            var colliderOffset = (Vector2)_ball.transform.TransformVector(_ballCollider.offset);
            _ballBody.position = (Vector2)bounds.center - colliderOffset;
            _ballBody.linearVelocity = Vector2.down * 8f;

            var deadline = Time.realtimeSinceStartup + 2f;
            while (_lives.RemainingLives == livesBeforeEntry && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_lives.RemainingLives, Is.EqualTo(livesBeforeEntry - 1),
                "DeathZone must consume exactly one life within 2 seconds.");
            yield return null;
        }

        private void OnScoreChanged(int total)
        {
            Assert.That(_score.Total, Is.EqualTo(total));
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Playing));
            _publishedTotals.Add(total);
        }
    }
}
