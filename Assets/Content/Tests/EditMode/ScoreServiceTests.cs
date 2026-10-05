using System;
using System.Collections.Generic;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class ScoreServiceTests
    {
        [Test]
        public void NewService_StartsAtZeroIndependentlyOfPreviousSession()
        {
            var previous = new ScoreService(new BaseScoreCalculator());
            previous.AddScore(100);

            var next = new ScoreService(new BaseScoreCalculator());

            Assert.That(previous.Total, Is.EqualTo(100));
            Assert.That(next.Total, Is.EqualTo(0));
        }

        [Test]
        public void AddScore_DelegatesOncePerAwardAndPublishesUpdatedCalculatedTotal()
        {
            var calculator = new ScoreTestSupport.RecordingCalculator(40);
            var service = new ScoreService(calculator);
            var published = new List<int>();
            service.ScoreChanged += total =>
            {
                Assert.That(service.Total, Is.EqualTo(total));
                published.Add(total);
            };

            service.AddScore(75);
            Assert.That(calculator.CallCount, Is.EqualTo(1));
            Assert.That(calculator.LastBaseScore, Is.EqualTo(75));
            service.AddScore(100);

            Assert.That(calculator.CallCount, Is.EqualTo(2));
            Assert.That(calculator.LastBaseScore, Is.EqualTo(100));
            Assert.That(service.Total, Is.EqualTo(80));
            Assert.That(published, Is.EqualTo(new[] { 40, 80 }));
        }

        [Test]
        public void AddScore_ZeroAward_PublishesCurrentTotal()
        {
            var service = new ScoreService(new BaseScoreCalculator());
            service.AddScore(100);
            var published = new List<int>();
            service.ScoreChanged += published.Add;

            service.AddScore(0);

            Assert.That(service.Total, Is.EqualTo(100));
            Assert.That(published, Is.EqualTo(new[] { 100 }));
        }

        [Test]
        public void AddScore_NegativeInput_PreservesTotalWithoutPublicationAndAllowsNextAward()
        {
            var service = new ScoreService(new BaseScoreCalculator());
            service.AddScore(100);
            var published = new List<int>();
            service.ScoreChanged += published.Add;

            Assert.Throws<ArgumentOutOfRangeException>(() => service.AddScore(-1));

            Assert.That(service.Total, Is.EqualTo(100));
            Assert.That(published, Is.Empty);

            service.AddScore(75);

            Assert.That(service.Total, Is.EqualTo(175));
            Assert.That(published, Is.EqualTo(new[] { 175 }));
        }

        [Test]
        public void AddScore_CalculationOverflow_PreservesTotalAndComboWithoutPublication()
        {
            var combo = ScoreTestSupport.CreateCombo(2);
            var service = new ScoreService(ScoreTestSupport.CreateComposition(combo, false));
            service.AddScore(100);
            var published = new List<int>();
            service.ScoreChanged += published.Add;

            Assert.Throws<OverflowException>(() => service.AddScore(int.MaxValue));

            Assert.That(service.Total, Is.EqualTo(200));
            Assert.That(combo.Count, Is.EqualTo(2));
            Assert.That(published, Is.Empty);
        }

        [Test]
        public void AddScore_TotalOverflow_PreservesPreviousTotalWithoutPublication()
        {
            var service = new ScoreService(new BaseScoreCalculator());
            service.AddScore(int.MaxValue);
            var published = new List<int>();
            service.ScoreChanged += published.Add;

            Assert.Throws<OverflowException>(() => service.AddScore(1));

            Assert.That(service.Total, Is.EqualTo(int.MaxValue));
            Assert.That(published, Is.Empty);
        }

        [Test]
        public void AddScore_ModifierChanges_AffectOnlyNewAwardsWithoutResettingTotal()
        {
            var combo = ScoreTestSupport.CreateCombo(3);
            var calculator = ScoreTestSupport.CreateComposition(combo, false);
            var service = new ScoreService(calculator);

            service.AddScore(100);
            Assert.That(service.Total, Is.EqualTo(300));
            Assert.That(combo.Count, Is.EqualTo(3));

            calculator.IsEnabled = true;
            service.AddScore(100);
            Assert.That(service.Total, Is.EqualTo(900));

            combo.Reset();
            calculator.IsEnabled = false;
            service.AddScore(100);

            Assert.That(service.Total, Is.EqualTo(1000));
            Assert.That(combo.Count, Is.EqualTo(0));
        }
    }
}
