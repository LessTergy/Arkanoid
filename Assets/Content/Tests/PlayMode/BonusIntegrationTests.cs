using System.Collections;
using System.Reflection;
using Arkanoid.Ball;
using Arkanoid.Bonus;
using Arkanoid.Bricks;
using Arkanoid.Core.Bonus;
using Arkanoid.Core.Bricks;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using Arkanoid.Input;
using Arkanoid.Levels;
using Arkanoid.Paddle;
using Arkanoid.Playfield;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class BonusIntegrationTests
    {
        private const int MaxTriggerPhysicsSteps = 5;

        private GameObject _root;
        private Texture2D _texture;
        private Sprite _sprite;
        private PaddleConfig _config;
        private PaddleMovement _paddle;
        private PlayfieldCamera _camera;
        private DeathZone _deathZone;
        private LevelView _level;
        private BonusDropDefinition _drop;
        private BrickDefinition _definition;
        private BrickView[] _bricks;
        private GameSession _session;
        private GameplayPauseController _pause;
        private GameplayBonusHandler _handler;
        private ExpandPaddleEffect _effect;
        private GameplayScoreHandler _scoreHandler;
        private ScoreService _score;
        private ComboModel _combo;
        private RecordingRandom _random;
        private IObjectResolver _resolver;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _root = new GameObject("Bonus tests");
            _root.transform.position = new Vector3(100f, 0f, 0f);
            _camera = NewObject("Camera").AddComponent<PlayfieldCamera>();
            _texture = new Texture2D(8, 8);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 8f, 8f), Vector2.one * 0.5f,
                100f, 0, SpriteMeshType.FullRect, Vector4.one);
            _config = ScriptableObject.CreateInstance<PaddleConfig>();
            var paddleObject = NewObject("Paddle");
            paddleObject.transform.localPosition = new Vector3(0f, -7f, 0f);
            var renderer = paddleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            _paddle = paddleObject.AddComponent<PaddleMovement>();
            paddleObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            _paddle.Construct(new IdleInput(), _config, _camera);
            _paddle.enabled = false;
            _paddle.SetWidth(_config.Width);
            _deathZone = NewObject("DeathZone").AddComponent<DeathZone>();
            _deathZone.transform.localPosition = new Vector3(0f, -9f, 0f);
            var deathCollider = _deathZone.gameObject.AddComponent<BoxCollider2D>();
            deathCollider.isTrigger = true;
            deathCollider.size = new Vector2(20f, 1f);
            _session = new GameSession();
            var inputObject = NewObject("Inactive input");
            inputObject.SetActive(false);
            _pause = new GameplayPauseController(inputObject.AddComponent<InputSystemPlayerInput>(), _session);
            _pause.Start();
            _level = NewObject("Level").AddComponent<LevelView>();
            var template = NewObject("Pickup template");
            template.transform.localPosition = new Vector3(0f, 30f, 0f);
            var pickupBody = template.AddComponent<Rigidbody2D>();
            pickupBody.bodyType = RigidbodyType2D.Kinematic;
            var pickupCollider = template.AddComponent<BoxCollider2D>();
            pickupCollider.isTrigger = true;
            var pickup = template.AddComponent<BonusPickup>();
            SetField(pickup, "_rigidbody", pickupBody);
            SetField(pickup, "_collider", pickupCollider);
            pickup.Construct(_paddle, _deathZone, new GameSession(), _pause);
            _drop = ScriptableObject.CreateInstance<BonusDropDefinition>();
            SetField(_drop, "_pickupPrefab", pickup);
            _definition = ScriptableObject.CreateInstance<BrickDefinition>();
            SetField(_definition, "_bonusDrop", _drop);
            _bricks = new[] { CreateBrick(), CreateBrick(), CreateBrick() };
            _level.Construct(new BrickHitProcessor(
                new IndestructibleHitHandler(new ShieldHitHandler(new DamageHitHandler()))));
            _random = new RecordingRandom();
            BuildBonusScope();
            _combo = new ComboModel();
            _score = new ScoreService(new ComboScoreDecorator(new BaseScoreCalculator(), _combo));
            _scoreHandler = new GameplayScoreHandler(_level, _session, _combo, _score);
            _scoreHandler.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver?.Dispose();
            _scoreHandler.Dispose();
            _pause.Dispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_drop);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
            Time.timeScale = _previousTimeScale;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NewDestruction_DropsOnceAtBrickPositionAndSkipsLastBeforeRandom(bool bonusFirst)
        {
            if (!bonusFirst)
            {
                _handler.Dispose();
                _handler.Start();
            }

            _level.Finished += _session.CompleteLevel;
            _session.TryStartPlaying();
            _bricks[0].transform.position = new Vector3(101f, 5f, 0f);
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            Assert.That(pickup.transform.position, Is.EqualTo(_bricks[0].transform.position));
            Assert.That(pickup.gameObject.activeSelf, Is.True);
            Assert.That(_drop.PickupPrefab.gameObject.activeSelf, Is.True);
            Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.True);
            Assert.That(pickup.GetComponent<BoxCollider2D>().enabled, Is.True);
            Assert.That(_bricks[0].Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
            Assert.That(_random.Calls, Is.EqualTo(1));
            _random.Value = 1f;
            _bricks[1].Hit();
            var scoreBeforeLast = _score.Total;
            _bricks[2].Hit();

            Assert.That(_random.Calls, Is.EqualTo(2));
            Assert.That(_level.GetComponentsInChildren<BonusPickup>().Length, Is.EqualTo(1));
            Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(pickup.GetComponent<BoxCollider2D>().enabled, Is.False);
            Assert.That(_session.State, Is.EqualTo(GameSessionState.LevelComplete));
            Assert.That(_score.Total, Is.GreaterThan(scoreBeforeLast));
        }

        [Test]
        public void ReadyAndPause_DoNotUseRandom()
        {
            _bricks[0].Hit();
            _session.TryStartPlaying();
            _pause.TogglePause();
            _bricks[1].Hit();
            _pause.Resume();
            Assert.That(_random.Calls, Is.Zero);
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [Test]
        public void AbsentProfile_DoesNotUseRandom()
        {
            _session.TryStartPlaying();
            SetField(_definition, "_bonusDrop", null);
            _bricks[0].Hit();
            Assert.That(_random.Calls, Is.Zero);
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Pickup_FallsAndCollectsOnceWithoutChangingScore()
        {
            _session.TryStartPlaying();
            _bricks[0].transform.position = new Vector3(100f, 4f, 0f);
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            var initialY = pickup.transform.position.y;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(pickup.transform.position.y, Is.LessThan(initialY));
            var collected = 0;
            pickup.Collected += _ => collected++;
            var total = _score.Total;
            var combo = _combo.Count;
            pickup.GetComponent<Rigidbody2D>().position = _paddle.GetComponent<Rigidbody2D>().position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(collected, Is.EqualTo(1));
            Assert.That(pickup == null, Is.True);
            Assert.That(_paddle.Width, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(_score.Total, Is.EqualTo(total));
            Assert.That(_combo.Count, Is.EqualTo(combo));
        }

        [UnityTest]
        public IEnumerator MissedPickup_IsRemovedWithoutBallEnteredOrExpansion()
        {
            _session.TryStartPlaying();
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            var ballEvents = 0;
            _deathZone.BallEntered += () => ballEvents++;
            pickup.GetComponent<Rigidbody2D>().position = _deathZone.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(pickup == null, Is.True);
            Assert.That(ballEvents, Is.Zero);
            Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
        }

        [UnityTest]
        public IEnumerator Pause_StopsFallAndCollectionUntilResume()
        {
            _session.TryStartPlaying();
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            _pause.TogglePause();
            var position = pickup.transform.position;
            yield return null;
            yield return null;
            Assert.That(pickup.transform.position, Is.EqualTo(position));
            pickup.GetComponent<Rigidbody2D>().position = _paddle.GetComponent<Rigidbody2D>().position;
            yield return null;
            Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
            _pause.Resume();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(_paddle.Width, Is.EqualTo(3f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator ScopeDisposal_RemovesPickupsAndUnsubscribesFromDestruction()
        {
            _session.TryStartPlaying();
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            _resolver.Dispose();
            _resolver = null;
            Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
            _paddle.SetWidth(2.5f);
            _session.TryLoseLife();
            Assert.That(_paddle.Width, Is.EqualTo(2.5f), "Disposed handlers must not reset a surviving owner.");
            _session.ResumeAfterLifeLoss();
            _session.TryStartPlaying();
            _bricks[1].Hit();
            Assert.That(_random.Calls, Is.EqualTo(1));
            yield return null;
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [TestCase(-1f, 2f)]
        [TestCase(1f, -2f)]
        public void Expansion_ClampsImmediatelyWithScaleAndOffsetAndDoesNotStack(float side, float scale)
        {
            _paddle.transform.localScale = new Vector3(scale, 1f, 1f);
            _paddle.Collider.offset = new Vector2(0.25f, 0f);
            var body = _paddle.GetComponent<Rigidbody2D>();
            body.position = new Vector2(_camera.WorldBounds.center.x + side * 10f, -7f);

            _effect.Apply();
            _effect.Apply();

            var bounds = _camera.WorldBounds;
            var expectedX = (side < 0f ? bounds.xMin + 1.5f : bounds.xMax - 1.5f) - 0.25f * scale;
            Assert.That(body.position.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(_paddle.transform.position.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(_paddle.Width, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(_paddle.GetComponent<SpriteRenderer>().size.x, Is.EqualTo(_paddle.Collider.size.x));
            Assert.That(_config.Width, Is.EqualTo(2f));
            Physics2D.SyncTransforms();
            Assert.That(_paddle.Collider.bounds.min.x, Is.GreaterThanOrEqualTo(bounds.xMin - 0.0001f));
            Assert.That(_paddle.Collider.bounds.max.x, Is.LessThanOrEqualTo(bounds.xMax + 0.0001f));
            var center = _paddle.Collider.bounds.center.x;
            var originalBounce = BallBounceCalculator.CalculatePaddleBounceDirection(center + 0.5f, center, 2f, 0.25f);
            var expandedBounce = BallBounceCalculator.CalculatePaddleBounceDirection(center + 0.75f, center, _paddle.Width, 0.25f);
            Assert.That(Vector2.Distance(originalBounce, expandedBounce), Is.LessThan(0.0001f));
        }

        [UnityTest]
        public IEnumerator LifeLost_ClearsPhysicsAndWidthBeforeSynchronousReady()
        {
            _session.TryStartPlaying();
            _bricks[0].Hit();
            var pickup = OnlyPickup();
            _effect.Apply();
            var total = _score.Total;
            var lifeLostEvents = 0;
            _session.StateChanged += state =>
            {
                if (state == GameSessionState.LifeLost)
                {
                    lifeLostEvents++;
                    Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
                    Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
                    Assert.That(pickup.GetComponent<BoxCollider2D>().enabled, Is.False);
                }
            };

            Assert.That(_session.TryLoseLife(), Is.True);
            _session.ResumeAfterLifeLoss();

            Assert.That(_session.State, Is.EqualTo(GameSessionState.Ready));
            Assert.That(lifeLostEvents, Is.EqualTo(1));
            Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
            Assert.That(_score.Total, Is.EqualTo(total));
            Assert.That(_combo.Count, Is.Zero);
            yield return null;
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator LastLife_PhysicalDeathZoneResetsAtLifeLostAndEndsGame()
        {
            var config = ScriptableObject.CreateInstance<BallConfig>();
            var ball = NewObject("Ball").AddComponent<BallController>();
            ball.Construct(_paddle, config);
            var ballBody = ball.GetComponent<Rigidbody2D>();
            var ballCollider = ball.GetComponent<CircleCollider2D>();
            var zoneCollider = _deathZone.GetComponent<BoxCollider2D>();
            _deathZone.Construct(ball);
            var lives = new LivesModel();
            lives.TryLoseLife();
            lives.TryLoseLife();
            var lifeLoss = new LifeLossHandler(_deathZone, lives, ball, _paddle, _session);
            lifeLoss.Start();
            try
            {
                yield return null;
                _session.TryStartPlaying();
                ball.Launch();
                yield return new WaitForFixedUpdate();
                yield return null;

                Assert.That(ball.State, Is.EqualTo(BallState.Flying));
                Assert.That(ballBody.simulated, Is.True);
                Assert.That(lives.RemainingLives, Is.EqualTo(1));
                _bricks[0].Hit();
                var pickup = OnlyPickup();
                _effect.Apply();
                var total = _score.Total;
                var resetAtLifeLost = false;
                _session.StateChanged += state =>
                {
                    if (state == GameSessionState.LifeLost)
                    {
                        resetAtLifeLost = true;
                        Assert.That(lives.RemainingLives, Is.Zero);
                        Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
                        Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
                    }
                };
                Physics2D.SyncTransforms();
                var zoneBounds = zoneCollider.bounds;
                var colliderOffset = (Vector2)ball.transform.TransformVector(ballCollider.offset);
                var entryPosition = (Vector2)zoneBounds.center - colliderOffset;
                ball.transform.position = new Vector3(entryPosition.x, entryPosition.y, ball.transform.position.z);
                ballBody.position = entryPosition;
                ballBody.linearVelocity = Vector2.down * config.Speed;
                Physics2D.SyncTransforms();

                for (var step = 0; step < MaxTriggerPhysicsSteps && !resetAtLifeLost; step++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(resetAtLifeLost, Is.True,
                    $"DeathZone did not cause LifeLost within {MaxTriggerPhysicsSteps} physics steps. "
                    + $"Ball position: {ballBody.position}, velocity: {ballBody.linearVelocity}, "
                    + $"simulated: {ballBody.simulated}, zone bounds: {zoneBounds}, "
                    + $"lives: {lives.RemainingLives}, session: {_session.State}.");
                Assert.That(_session.State, Is.EqualTo(GameSessionState.GameOver));
                Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
                Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
                Assert.That(_score.Total, Is.EqualTo(total));
                Assert.That(_combo.Count, Is.Zero);
            }
            finally
            {
                lifeLoss.Dispose();
                Object.DestroyImmediate(ball.gameObject);
                Object.DestroyImmediate(config);
            }
        }

        [UnityTest]
        public IEnumerator Victory_ClearsAllPickupsAndWidthAndRestoresPausedTime()
        {
            _session.TryStartPlaying();
            _bricks[0].Hit();
            _bricks[1].Hit();
            var pickups = _level.GetComponentsInChildren<BonusPickup>();
            Assert.That(pickups.Length, Is.EqualTo(2));
            _effect.Apply();
            _pause.TogglePause();
            var total = _score.Total;
            var combo = _combo.Count;
            _session.CompleteLevel();

            Assert.That(_pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_paddle.Width, Is.EqualTo(_config.Width));
            foreach (var pickup in pickups)
            {
                Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
                Assert.That(pickup.GetComponent<BoxCollider2D>().enabled, Is.False);
            }

            Assert.That(_score.Total, Is.EqualTo(total));
            Assert.That(_combo.Count, Is.EqualTo(combo));
            yield return null;
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ScopeRecreation_TwiceLeavesOneHandlerAndNoOldPickups()
        {
            _session.TryStartPlaying();
            for (var round = 0; round < 2; round++)
            {
                _bricks[round].Hit();
                var pickup = OnlyPickup();
                Assert.That(_random.Calls, Is.EqualTo(round + 1));
                _resolver.Dispose();
                _resolver = null;
                Assert.That(pickup.GetComponent<Rigidbody2D>().simulated, Is.False);
                yield return null;
                Assert.That(pickup == null, Is.True);
                BuildBonusScope();
            }

            _bricks[2].Hit();
            Assert.That(_random.Calls, Is.EqualTo(2));
            Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ShieldDamageAndIndestructible_DoNotDropUntilUniqueDestruction()
        {
            _resolver.Dispose();
            _resolver = null;
            _scoreHandler.Dispose();
            _level = NewObject("Protected level").AddComponent<LevelView>();
            var protectedDefinition = ScriptableObject.CreateInstance<BrickDefinition>();
            var indestructibleDefinition = ScriptableObject.CreateInstance<BrickDefinition>();
            try
            {
                SetField(protectedDefinition, "_maxHealth", 3);
                SetField(protectedDefinition, "_shieldCharges", 1);
                SetField(protectedDefinition, "_bonusDrop", _drop);
                SetField(indestructibleDefinition, "_isIndestructible", true);
                SetField(indestructibleDefinition, "_bonusDrop", _drop);
                var target = CreateBrick(protectedDefinition);
                var indestructible = CreateBrick(indestructibleDefinition);
                CreateBrick();
                _level.Construct(new BrickHitProcessor(
                    new IndestructibleHitHandler(new ShieldHitHandler(new DamageHitHandler()))));
                BuildBonusScope();
                _session.TryStartPlaying();

                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.ShieldBroken));
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
                Assert.That(indestructible.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Indestructible));
                Assert.That(_random.Calls, Is.Zero);
                Assert.That(_level.GetComponentsInChildren<BonusPickup>(), Is.Empty);
                Assert.That(_level.RemainingBricks, Is.EqualTo(2));
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
                Assert.That(_random.Calls, Is.EqualTo(1));
                OnlyPickup();
                Assert.That(protectedDefinition.CreateSettings().MaxHealth, Is.EqualTo(3));
                Assert.That(protectedDefinition.CreateSettings().ShieldCharges, Is.EqualTo(1));
                yield return null;
            }
            finally
            {
                Object.DestroyImmediate(protectedDefinition);
                Object.DestroyImmediate(indestructibleDefinition);
            }
        }

        private void BuildBonusScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_paddle);
            builder.RegisterInstance(_config);
            builder.RegisterInstance(_deathZone);
            builder.RegisterInstance(_session);
            builder.RegisterInstance(_pause);
            builder.RegisterInstance(_level);
            builder.RegisterInstance(_random).As<IRandomProvider>();
            builder.Register<BonusDropService>(Lifetime.Scoped);
            builder.Register<BonusFactory>(Lifetime.Scoped);
            builder.Register<ExpandPaddleEffect>(Lifetime.Scoped);
            builder.Register<GameplayBonusHandler>(Lifetime.Scoped);
            _resolver = builder.Build();
            _handler = _resolver.Resolve<GameplayBonusHandler>();
            _effect = _resolver.Resolve<ExpandPaddleEffect>();
            _handler.Start();
        }

        private BonusPickup OnlyPickup()
        {
            var pickups = _level.GetComponentsInChildren<BonusPickup>();
            Assert.That(pickups.Length, Is.EqualTo(1));
            return pickups[0];
        }

        private GameObject NewObject(string name)
        {
            var instance = new GameObject(name);
            instance.transform.SetParent(_root.transform, false);
            return instance;
        }

        private BrickView CreateBrick(BrickDefinition definition = null)
        {
            var instance = NewObject("Brick");
            instance.transform.SetParent(_level.transform, false);
            var brick = instance.AddComponent<BrickView>();
            SetField(brick, "_definition", definition != null ? definition : _definition);
            SetField(brick, "_bodyRenderer", instance.AddComponent<SpriteRenderer>());
            var shield = NewObject("Shield");
            shield.transform.SetParent(instance.transform, false);
            SetField(brick, "_shieldRenderer", shield.AddComponent<SpriteRenderer>());
            var label = NewObject("Label");
            label.transform.SetParent(instance.transform, false);
            SetField(brick, "_stateLabel", label.AddComponent<TextMeshPro>());
            return brick;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class RecordingRandom : IRandomProvider
        {
            public int Calls { get; private set; }
            public float Value { get; set; } = 0.125f;

            public float NextFloat01()
            {
                Calls++;
                return Value;
            }
        }

        private sealed class IdleInput : IPlayerInput
        {
            public PlayerMoveIntent Move { get { return default; } }
            public bool LaunchPressedThisFrame { get { return false; } }
            public bool PausePressedThisFrame { get { return false; } }
        }
    }
}
