using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Arkanoid.Bricks;
using Arkanoid.Core.Bricks;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using Arkanoid.Levels;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class BrickHitIntegrationTests
    {
        private GameObject _levelObject;
        private LevelView _level;
        private BrickDefinition _definition;
        private readonly List<BrickDefinition> _extraDefinitions = new();
        private readonly List<int> _publishedTotals = new();
        private GameSession _session;
        private ComboModel _combo;
        private ScoreService _score;
        private GameplayScoreHandler _scoreHandler;

        [SetUp]
        public void SetUp()
        {
            _levelObject = new GameObject("Brick hit test level");
            _level = _levelObject.AddComponent<LevelView>();
            _definition = ScriptableObject.CreateInstance<BrickDefinition>();
            _publishedTotals.Clear();
            _session = new GameSession();
            _combo = new ComboModel();
            _score = new ScoreService(new DoubleScoreDecorator(new ComboScoreDecorator(new BaseScoreCalculator(), _combo)));
            _score.ScoreChanged += total => _publishedTotals.Add(total);
            _scoreHandler = new GameplayScoreHandler(_level, _session, _combo, _score);
            _scoreHandler.Start();
        }

        [UnityTest]
        public IEnumerator ShieldedDurable_NonfatalHitsDoNotReduceLevelAndDestructionIsUnique()
        {
            SetField(_definition, "_maxHealth", 3);
            SetField(_definition, "_shieldCharges", 1);
            SetField(_definition, "_baseScore", 200);
            var brick = CreateBrick();
            var label = brick.GetComponentInChildren<TextMeshPro>();
            var shield = brick.transform.GetChild(0).GetComponent<SpriteRenderer>();
            var destroyed = 0;
            var finished = 0;
            var observedBaseScore = -1;
            _level.BrickDestroyed += target =>
            {
                destroyed++;
                observedBaseScore = target.State.Settings.BaseScore;
                Assert.That(target.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored),
                    "A repeated hit during destruction must not republish the event.");
            };
            _level.Finished += () =>
            {
                finished++;
                Assert.That(destroyed, Is.EqualTo(1));
                Assert.That(observedBaseScore, Is.EqualTo(200));
            };

            _level.Construct(CreateProcessor());
            Assert.That(brick.State.CurrentHealth, Is.EqualTo(3));
            Assert.That(_level.RemainingBricks, Is.EqualTo(1));
            Assert.That(label.text, Is.EqualTo("HP 3 | S 1"));
            Assert.That(shield.enabled, Is.True);

            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.ShieldBroken));
            Assert.That(brick.State.CurrentHealth, Is.EqualTo(3));
            Assert.That(label.text, Is.EqualTo("HP 3"));
            Assert.That(shield.enabled, Is.False);
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(label.text, Is.EqualTo("HP 2"));
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(_level.RemainingBricks, Is.EqualTo(1));
            Assert.That(destroyed, Is.Zero);
            Assert.That(finished, Is.Zero);

            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
            Assert.That(_level.RemainingBricks, Is.Zero);
            Assert.That(destroyed, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(_definition.CreateSettings().MaxHealth, Is.EqualTo(3));
            Assert.That(_definition.CreateSettings().ShieldCharges, Is.EqualTo(1));

            yield return null;
            Assert.That(brick == null, Is.True, "Destruction must remove the GameObject after the frame.");
        }

        [UnityTest]
        public IEnumerator LevelInjection_InitializesIndependentStatesBeforeTracking()
        {
            SetField(_definition, "_maxHealth", 3);
            var first = CreateBrick();
            var second = CreateBrick();

            _level.Construct(CreateProcessor());

            Assert.That(_level.RemainingBricks, Is.EqualTo(2));
            Assert.That(first.State, Is.Not.SameAs(second.State));
            Assert.That(first.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(first.State.CurrentHealth, Is.EqualTo(2));
            Assert.That(second.State.CurrentHealth, Is.EqualTo(3));
            Assert.That(_definition.CreateSettings().MaxHealth, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => first.Initialize(CreateProcessor()));
            Assert.That(first.State.CurrentHealth, Is.EqualTo(2), "Reinitialization must not restore health.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Presentation_UsesDefinitionColorAndUpdatesMultipleShieldCharges()
        {
            SetField(_definition, "_color", Color.cyan);
            SetField(_definition, "_shieldCharges", 2);
            var brick = CreateBrick();
            var label = brick.GetComponentInChildren<TextMeshPro>();
            var shield = brick.transform.GetChild(0).GetComponent<SpriteRenderer>();
            _level.Construct(CreateProcessor());

            Assert.That(brick.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.cyan));
            Assert.That(label.text, Is.EqualTo("HP 1 | S 2"));
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.ShieldAbsorbed));
            Assert.That(shield.enabled, Is.True);
            Assert.That(label.text, Is.EqualTo("HP 1 | S 1"));
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.ShieldBroken));
            Assert.That(shield.enabled, Is.False);
            Assert.That(label.text, Is.EqualTo("HP 1"));
            Assert.That(_definition.CreateSettings().ShieldCharges, Is.EqualTo(2));
            yield return null;
        }

        [TestCase("_baseScore", -1, "baseScore")]
        [TestCase("_maxHealth", 0, "maxHealth")]
        [TestCase("_maxHealth", 6, "maxHealth")]
        [TestCase("_shieldCharges", -1, "shieldCharges")]
        public void InvalidDefinition_IsRejectedBeforeScoreComboAndLevelEvents(string field, int value, string parameter)
        {
            Assert.That(_session.TryStartPlaying(), Is.True);
            _combo.Advance();
            _combo.Advance();
            _score.AddScore(100);
            _publishedTotals.Clear();
            SetField(_definition, field, value);
            var brick = CreateBrick();
            var destroyed = 0;
            var finished = 0;
            _level.BrickDestroyed += _ => destroyed++;
            _level.Finished += () => finished++;

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _level.Construct(CreateProcessor()));

            Assert.That(exception.ParamName, Is.EqualTo(parameter));
            Assert.That(brick.State, Is.Null);
            Assert.That(_level.RemainingBricks, Is.Zero);
            Assert.That(destroyed, Is.Zero);
            Assert.That(finished, Is.Zero);
            Assert.That(_score.Total, Is.EqualTo(200));
            Assert.That(_combo.Count, Is.EqualTo(2));
            Assert.That(_publishedTotals, Is.Empty);

            SetField(_definition, "_baseScore", 0);
            SetField(_definition, "_maxHealth", 1);
            SetField(_definition, "_shieldCharges", 0);
            _level.Construct(CreateProcessor());
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(brick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
            Assert.That(_score.Total, Is.EqualTo(200));
            Assert.That(_combo.Count, Is.EqualTo(3));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 200 }));
            Assert.That(destroyed, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Destruction_OutsidePlayingDoesNotAwardScoreOrAdvanceCombo()
        {
            var readyBrick = CreateBrick();
            var playingBrick = CreateBrick();
            _level.Construct(CreateProcessor());

            Assert.That(readyBrick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(_session.State, Is.EqualTo(GameSessionState.Ready));
            Assert.That(_score.Total, Is.Zero);
            Assert.That(_combo.Count, Is.Zero);
            Assert.That(_publishedTotals, Is.Empty);
            Assert.That(_level.RemainingBricks, Is.EqualTo(1));

            Assert.That(_session.TryStartPlaying(), Is.True);
            Assert.That(playingBrick.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(_score.Total, Is.EqualTo(100));
            Assert.That(_combo.Count, Is.EqualTo(1));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100 }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyLevel_IsRejectedDuringInitialization()
        {
            Assert.Throws<InvalidOperationException>(() => _level.Construct(CreateProcessor()));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MixedLevel_CompletesAfterDestructibleBricksAndKeepsIndestructibleIntact()
        {
            Assert.That(_session.TryStartPlaying(), Is.True);
            var normal = CreateBrick();
            var durable = CreateBrick(CreateDefinition(200, 3, 0, false));
            var shielded = CreateBrick(CreateDefinition(150, 1, 1, false));
            var indestructible = CreateBrick(CreateDefinition(0, 1, 2, true));
            var destructionScores = new List<int>();
            var finished = 0;
            _level.BrickDestroyed += brick => destructionScores.Add(brick.State.Settings.BaseScore);
            _level.Finished += () =>
            {
                finished++;
                Assert.That(destructionScores, Is.EqualTo(new[] { 100, 200, 150 }),
                    "The final destruction event must precede Finished.");
                Assert.That(_score.Total, Is.EqualTo(950));
                Assert.That(_combo.Count, Is.EqualTo(3));
                Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 500, 950 }));
                Assert.That(_session.State, Is.EqualTo(GameSessionState.Playing));
                _session.CompleteLevel();
            };
            _level.Construct(CreateProcessor());

            Assert.That(_level.RemainingBricks, Is.EqualTo(3));
            var indestructibleLabel = indestructible.GetComponentInChildren<TextMeshPro>();
            Assert.That(indestructibleLabel.text, Is.EqualTo("HP INF | S 2"));
            for (var hit = 0; hit < 5; hit++)
            {
                Assert.That(indestructible.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Indestructible));
            }

            Assert.That(indestructible.State.CurrentHealth, Is.EqualTo(1));
            Assert.That(indestructible.State.CurrentShieldCharges, Is.EqualTo(2));
            Assert.That(destructionScores, Is.Empty);
            Assert.That(_score.Total, Is.Zero);
            Assert.That(_combo.Count, Is.Zero);
            Assert.That(_publishedTotals, Is.Empty);
            Assert.That(_level.RemainingBricks, Is.EqualTo(3));
            Assert.That(normal.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(_level.RemainingBricks, Is.EqualTo(2));
            Assert.That(durable.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(durable.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Damaged));
            Assert.That(_level.RemainingBricks, Is.EqualTo(2));
            Assert.That(destructionScores, Is.EqualTo(new[] { 100 }));
            Assert.That(_score.Total, Is.EqualTo(100));
            Assert.That(_combo.Count, Is.EqualTo(1));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100 }));
            Assert.That(durable.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(_level.RemainingBricks, Is.EqualTo(1));
            Assert.That(shielded.Hit().Outcome, Is.EqualTo(BrickHitOutcome.ShieldBroken));
            Assert.That(_level.RemainingBricks, Is.EqualTo(1));
            Assert.That(finished, Is.Zero);
            Assert.That(_score.Total, Is.EqualTo(500));
            Assert.That(_combo.Count, Is.EqualTo(2));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 500 }));
            Assert.That(shielded.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Destroyed));
            Assert.That(_level.RemainingBricks, Is.Zero);
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(shielded.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Ignored));
            Assert.That(indestructible.Hit().Outcome, Is.EqualTo(BrickHitOutcome.Indestructible));
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(destructionScores, Is.EqualTo(new[] { 100, 200, 150 }));
            Assert.That(_score.Total, Is.EqualTo(950));
            Assert.That(_combo.Count, Is.EqualTo(3));
            Assert.That(_publishedTotals, Is.EqualTo(new[] { 100, 500, 950 }));
            Assert.That(_session.State, Is.EqualTo(GameSessionState.LevelComplete));
            yield return null;
            Assert.That(indestructible != null, Is.True);
            Assert.That(indestructibleLabel.text, Is.EqualTo("HP INF | S 2"));
        }

        [UnityTest]
        public IEnumerator IndestructibleOnlyLevel_WithInactiveNormal_IsRejectedWithoutFinishing()
        {
            SetField(_definition, "_isIndestructible", true);
            var indestructible = CreateBrick();
            var inactiveNormal = CreateBrick(CreateDefinition(100, 1, 0, false));
            inactiveNormal.gameObject.SetActive(false);
            var finished = 0;
            _level.Finished += () => finished++;

            var exception = Assert.Throws<InvalidOperationException>(() => _level.Construct(CreateProcessor()));

            Assert.That(exception.Message, Does.Contain("active destructible"));
            Assert.That(_level.RemainingBricks, Is.Zero);
            Assert.That(finished, Is.Zero);
            Assert.That(indestructible.State, Is.Not.Null);
            Assert.That(inactiveNormal.State, Is.Null);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _scoreHandler.Dispose();
            Object.Destroy(_levelObject);
            Object.Destroy(_definition);
            foreach (var definition in _extraDefinitions)
            {
                Object.Destroy(definition);
            }

            _extraDefinitions.Clear();
            yield return null;
        }

        private BrickView CreateBrick(BrickDefinition definition = null)
        {
            var brickObject = new GameObject("Test brick");
            brickObject.transform.SetParent(_levelObject.transform);
            var brick = brickObject.AddComponent<BrickView>();
            var body = brickObject.AddComponent<SpriteRenderer>();
            var shieldObject = new GameObject("Shield");
            shieldObject.transform.SetParent(brickObject.transform);
            var shield = shieldObject.AddComponent<SpriteRenderer>();
            var labelObject = new GameObject("State label");
            labelObject.transform.SetParent(brickObject.transform);
            var label = labelObject.AddComponent<TextMeshPro>();
            SetField(brick, "_definition", definition != null ? definition : _definition);
            SetField(brick, "_bodyRenderer", body);
            SetField(brick, "_shieldRenderer", shield);
            SetField(brick, "_stateLabel", label);
            return brick;
        }

        private BrickDefinition CreateDefinition(int baseScore, int health, int shields, bool indestructible)
        {
            var definition = ScriptableObject.CreateInstance<BrickDefinition>();
            _extraDefinitions.Add(definition);
            SetField(definition, "_baseScore", baseScore);
            SetField(definition, "_maxHealth", health);
            SetField(definition, "_shieldCharges", shields);
            SetField(definition, "_isIndestructible", indestructible);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Serialized field {fieldName} must exist.");
            field.SetValue(target, value);
        }

        private static BrickHitProcessor CreateProcessor()
        {
            return new BrickHitProcessor(new IndestructibleHitHandler(new ShieldHitHandler(new DamageHitHandler())));
        }
    }
}
