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
        private BrickView[] _allBricks;
        private BrickView[] _targets;
        private Dictionary<BrickView, BrickCollisionProbe> _probes;

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

            _allBricks = _level.GetComponentsInChildren<BrickView>();
            _probes = BrickHitTestSupport.Prepare(_allBricks);
            var destructible = new List<BrickView>();
            foreach (var brick in _allBricks)
            {
                if (!brick.State.Settings.IsIndestructible)
                {
                    destructible.Add(brick);
                }
            }

            Assert.That(destructible.Count, Is.GreaterThanOrEqualTo(3),
                "Score scenarios require at least three active destructible bricks.");
            Assert.That(_level.RemainingBricks, Is.EqualTo(destructible.Count));
            var durable = destructible.Find(brick => brick.State.Settings.MaxHealth > 1);
            Assert.That(durable, Is.Not.Null, "Include a brick with HP > 1 to test damage across LifeLost.");
            destructible.Remove(durable);
            _targets = new[] { destructible[0], destructible[1], durable };

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
            var firstScore = _targets[0].State.Settings.BaseScore;
            var seriesTotal = firstScore + _targets[1].State.Settings.BaseScore * 2;
            var nextScore = _targets[2].State.Settings.BaseScore;
            var damagedState = _targets[2].State;
            yield return DestroyBrick(0);
            yield return DestroyBrick(1);
            while (damagedState.CurrentShieldCharges > 0)
            {
                yield return BrickHitTestSupport.Hit(_ball, _targets[2], _probes[_targets[2]]);
            }

            yield return BrickHitTestSupport.Hit(_ball, _targets[2], _probes[_targets[2]]);
            var healthBeforeLifeLost = damagedState.CurrentHealth;
            var shieldsBeforeLifeLost = damagedState.CurrentShieldCharges;
            Assert.That(healthBeforeLifeLost, Is.EqualTo(damagedState.Settings.MaxHealth - 1));
            Assert.That(_score.Total, Is.EqualTo(seriesTotal));
            Assert.That(_combo.Count, Is.EqualTo(2));

            var lifeLostObserved = false;
            _session.StateChanged += state =>
            {
                if (state == GameSessionState.LifeLost)
                {
                    lifeLostObserved = true;
                    Assert.That(_combo.Count, Is.EqualTo(0));
                    Assert.That(_score.Total, Is.EqualTo(seriesTotal));
                }
            };

            yield return EnterDeathZone();

            Assert.That(lifeLostObserved, Is.True);
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Ready));
            Assert.That(_ball.State, Is.EqualTo(BallState.Attached));
            Assert.That(_targets[2].State, Is.SameAs(damagedState));
            Assert.That(damagedState.CurrentHealth, Is.EqualTo(healthBeforeLifeLost));
            Assert.That(damagedState.CurrentShieldCharges, Is.EqualTo(shieldsBeforeLifeLost));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { firstScore, seriesTotal }),
                "Losing a life must not award points or publish a score change.");

            yield return Launch();
            yield return DestroyBrick(2);

            Assert.That(_score.Total, Is.EqualTo(seriesTotal + nextScore));
            Assert.That(_combo.Count, Is.EqualTo(1));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { firstScore, seriesTotal, seriesTotal + nextScore }));
        }

        [UnityTest]
        public IEnumerator NegativeInput_InGameplayComposition_PreservesStateAndNextDestructionUsesNextCombo()
        {
            yield return Launch();
            var firstScore = _targets[0].State.Settings.BaseScore;
            var seriesTotal = firstScore + _targets[1].State.Settings.BaseScore * 2;
            var nextScore = _targets[2].State.Settings.BaseScore;
            yield return DestroyBrick(0);
            yield return DestroyBrick(1);
            _doubleScore.IsEnabled = true;
            var remainingBeforeRejection = _level.RemainingBricks;

            var exception = Assert.Throws<System.ArgumentOutOfRangeException>(() => _score.AddScore(-100));

            Assert.That(exception.ParamName, Is.EqualTo("baseScore"));
            Assert.That(_score.Total, Is.EqualTo(seriesTotal));
            Assert.That(_combo.Count, Is.EqualTo(2));
            Assert.That(_doubleScore.IsEnabled, Is.True);
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_level.RemainingBricks, Is.EqualTo(remainingBeforeRejection));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { firstScore, seriesTotal }));

            yield return DestroyBrick(2);

            Assert.That(_score.Total, Is.EqualTo(seriesTotal + nextScore * 3 * 2));
            Assert.That(_combo.Count, Is.EqualTo(3));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { firstScore, seriesTotal, seriesTotal + nextScore * 3 * 2 }));
        }

        [UnityTest]
        public IEnumerator ShieldDamageAndIndestructible_PhysicalHitsDoNotChangeScoreOrCombo()
        {
            yield return Launch();
            var remaining = _level.RemainingBricks;
            var sawShield = false;
            var sawDamage = false;
            var sawIndestructible = false;
            foreach (var brick in _allBricks)
            {
                var state = brick.State;
                if (state.Settings.IsIndestructible)
                {
                    sawIndestructible = true;
                    yield return BrickHitTestSupport.Hit(_ball, brick, _probes[brick]);
                    Assert.That(state.CurrentHealth, Is.EqualTo(state.Settings.MaxHealth));
                    Assert.That(state.CurrentShieldCharges, Is.EqualTo(state.Settings.ShieldCharges));
                }
                else
                {
                    while (state.CurrentShieldCharges > 0)
                    {
                        sawShield = true;
                        var shields = state.CurrentShieldCharges;
                        yield return BrickHitTestSupport.Hit(_ball, brick, _probes[brick]);
                        Assert.That(state.CurrentShieldCharges, Is.EqualTo(shields - 1));
                        Assert.That(state.CurrentHealth, Is.EqualTo(state.Settings.MaxHealth));
                    }

                    if (state.CurrentHealth > 1)
                    {
                        sawDamage = true;
                        yield return BrickHitTestSupport.Hit(_ball, brick, _probes[brick]);
                        Assert.That(state.CurrentHealth, Is.EqualTo(state.Settings.MaxHealth - 1));
                    }
                }

                Assert.That(_score.Total, Is.Zero);
                Assert.That(_combo.Count, Is.Zero);
                Assert.That(_publishedTotals, Is.Empty);
                Assert.That(_level.RemainingBricks, Is.EqualTo(remaining));
            }

            Assert.That(sawShield, Is.True, "Include a shielded brick in the gameplay test level.");
            Assert.That(sawDamage, Is.True);
            Assert.That(sawIndestructible, Is.True, "Include an indestructible brick in the gameplay test level.");
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
            var brick = _targets[index];
            var state = brick.State;
            var retryCount = 0;
            brick.Destroyed += target =>
            {
                retryCount++;
                var total = _score.Total;
                var combo = _combo.Count;
                var events = _publishedTotals.Count;
                var remaining = _level.RemainingBricks;
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
                Assert.That(_score.Total, Is.EqualTo(total));
                Assert.That(_combo.Count, Is.EqualTo(combo));
                Assert.That(_publishedTotals.Count, Is.EqualTo(events));
                Assert.That(_level.RemainingBricks, Is.EqualTo(remaining));
            };

            var totalBefore = _score.Total;
            var comboBefore = _combo.Count;
            var eventsBefore = _publishedTotals.Count;
            var remainingBefore = _level.RemainingBricks;
            while (!state.IsDestroyed)
            {
                var health = state.CurrentHealth;
                var shields = state.CurrentShieldCharges;
                yield return BrickHitTestSupport.Hit(_ball, brick, _probes[brick]);
                Assert.That(state.CurrentHealth, Is.EqualTo(shields > 0 ? health : health - 1));
                Assert.That(state.CurrentShieldCharges, Is.EqualTo(shields > 0 ? shields - 1 : 0));
                if (!state.IsDestroyed)
                {
                    Assert.That(_score.Total, Is.EqualTo(totalBefore));
                    Assert.That(_combo.Count, Is.EqualTo(comboBefore));
                    Assert.That(_publishedTotals.Count, Is.EqualTo(eventsBefore));
                    Assert.That(_level.RemainingBricks, Is.EqualTo(remainingBefore));
                }
            }

            var expectedCombo = Mathf.Min(comboBefore + 1, 5);
            var award = state.Settings.BaseScore * expectedCombo * (_doubleScore.IsEnabled ? 2 : 1);
            Assert.That(_score.Total, Is.EqualTo(totalBefore + award));
            Assert.That(_combo.Count, Is.EqualTo(expectedCombo));
            Assert.That(_publishedTotals.Count, Is.EqualTo(eventsBefore + 1));
            Assert.That(_level.RemainingBricks, Is.EqualTo(remainingBefore - 1));
            Assert.That(retryCount, Is.EqualTo(1));
        }

        private IEnumerator EnterDeathZone()
        {
            var livesBeforeEntry = _lives.RemainingLives;
            var bounds = _zoneCollider.bounds;
            Assert.That(_zoneCollider.isTrigger, Is.True);
            Assert.That(bounds.size.y, Is.GreaterThan(0f));
            var colliderOffset = (Vector2)_ball.transform.TransformVector(_ballCollider.offset);
            BrickHitTestSupport.ResumePhysicsAt(_ballBody, (Vector2)bounds.center - colliderOffset, Vector2.down * 8f);

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
