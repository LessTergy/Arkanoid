using System.Collections.Generic;
using Arkanoid.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusEffectTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Bonus effect");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void AddLife_UsesInjectedLivesAndStopsAtMaximum()
        {
            var lives = new LivesModel();
            var changes = new List<int>();
            lives.LivesChanged += changes.Add;
            var effect = _root.AddComponent<AddLifeEffect>();
            effect.Construct(lives);

            effect.Apply();
            effect.Apply();
            effect.Apply();

            Assert.That(lives.RemainingLives, Is.EqualTo(5));
            Assert.That(changes, Is.EqualTo(new[] { 4, 5 }));
        }

        [Test]
        public void AddLife_DoesNotReviveAtZero()
        {
            var lives = new LivesModel();
            while (lives.TryLoseLife())
            {
            }

            var effect = _root.AddComponent<AddLifeEffect>();
            effect.Construct(lives);
            effect.Apply();

            Assert.That(lives.RemainingLives, Is.Zero);
        }

        [Test]
        public void EnableDoubleScore_UsesInjectedCalculatorWithoutStackingOrAwardingScore()
        {
            var combo = new ComboModel();
            for (var i = 0; i < 3; i++)
            {
                combo.Advance();
            }

            var doubleScore = new DoubleScoreDecorator(new ComboScoreDecorator(new BaseScoreCalculator(), combo));
            var score = new ScoreService(doubleScore);
            var effect = _root.AddComponent<EnableDoubleScoreEffect>();
            effect.Construct(doubleScore);
            score.AddScore(100);

            effect.Apply();
            effect.Apply();

            Assert.That(score.Total, Is.EqualTo(300));
            Assert.That(combo.Count, Is.EqualTo(3));
            Assert.That(doubleScore.IsEnabled, Is.True);
            score.AddScore(100);
            Assert.That(score.Total, Is.EqualTo(900));
        }
    }
}
