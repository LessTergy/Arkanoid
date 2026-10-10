using System;
using System.Collections.Generic;
using Arkanoid.Core.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class CompositeBonusEffectTests
    {
        private LivesModel _lives;
        private DoubleScoreDecorator _doubleScore;
        private BonusContext _context;
        private int _expansionCalls;

        [SetUp]
        public void SetUp()
        {
            _lives = new LivesModel();
            _doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator());
            _expansionCalls = 0;
            _context = new BonusContext(_lives, _doubleScore, () => _expansionCalls++);
        }

        [Test]
        public void FlatAndNestedGroups_ApplyEachEntryInOrderWithSameContext()
        {
            var calls = new List<string>();
            var contexts = new List<BonusContext>();
            var expand = Record("expand", calls, contexts);
            var life = Record("life", calls, contexts);
            var doubleScore = Record("double", calls, contexts);
            IBonusEffect childGroup = new CompositeBonusEffect(new[] { expand, life });
            IBonusEffect parentGroup = new CompositeBonusEffect(new[] { childGroup, doubleScore });

            childGroup.Apply(_context);
            Assert.That(calls, Is.EqualTo(new[] { "expand", "life" }));
            calls.Clear();
            contexts.Clear();
            parentGroup.Apply(_context);

            Assert.That(calls, Is.EqualTo(new[] { "expand", "life", "double" }));
            Assert.That(contexts.Count, Is.EqualTo(3));
            foreach (var context in contexts)
            {
                Assert.That(context, Is.SameAs(_context));
            }
        }

        [Test]
        public void NestedGroup_AtMaximumLivesStillAppliesFollowingDoubleScore()
        {
            IBonusEffect childGroup = new CompositeBonusEffect(new IBonusEffect[]
            {
                new ExpandPaddleEffect(), new AddLifeEffect()
            });
            IBonusEffect parentGroup = new CompositeBonusEffect(new[] { childGroup, new EnableDoubleScoreEffect() });
            var lifeChanges = new List<int>();
            _lives.LivesChanged += lifeChanges.Add;

            parentGroup.Apply(_context);
            parentGroup.Apply(_context);
            _doubleScore.IsEnabled = false;
            parentGroup.Apply(_context);

            Assert.That(_expansionCalls, Is.EqualTo(3));
            Assert.That(_lives.RemainingLives, Is.EqualTo(5));
            Assert.That(lifeChanges, Is.EqualTo(new[] { 4, 5 }));
            Assert.That(_doubleScore.IsEnabled, Is.True);
            Assert.That(_doubleScore.Calculate(100), Is.EqualTo(200));
        }

        [Test]
        public void SourceMutation_DoesNotChangeValidatedSnapshot()
        {
            var calls = new List<string>();
            var contexts = new List<BonusContext>();
            var effects = new List<IBonusEffect>
            {
                Record("first", calls, contexts), Record("second", calls, contexts)
            };
            var composite = new CompositeBonusEffect(effects);
            effects[0] = null;
            effects.Clear();

            composite.Apply(_context);

            Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void ReusedLeaf_IsAppliedForEachEntryWithoutDeduplication()
        {
            var calls = new List<string>();
            var contexts = new List<BonusContext>();
            var shared = Record("shared", calls, contexts);
            var composite = new CompositeBonusEffect(new[] { shared, shared });

            composite.Apply(_context);

            Assert.That(calls, Is.EqualTo(new[] { "shared", "shared" }));
        }

        [Test]
        public void ChildFailure_IsPropagatedAndStopsRemainingEffects()
        {
            var calls = new List<string>();
            var contexts = new List<BonusContext>();
            var failure = new InvalidOperationException("Expected child failure.");
            var composite = new CompositeBonusEffect(new[]
            {
                Record("first", calls, contexts),
                new RecordingEffect(_ => throw failure),
                Record("last", calls, contexts)
            });

            Assert.That(Assert.Throws<InvalidOperationException>(() => composite.Apply(_context)), Is.SameAs(failure));
            Assert.That(calls, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void NullList_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new CompositeBonusEffect(null));
        }

        [Test]
        public void EmptyList_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new CompositeBonusEffect(Array.Empty<IBonusEffect>()));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void NullChild_IsRejectedBeforeApplyingAnyEffect(int index)
        {
            var calls = 0;
            var child = new RecordingEffect(_ => calls++);
            var effects = new IBonusEffect[] { child, child };
            effects[index] = null;

            Assert.Throws<ArgumentException>(() => new CompositeBonusEffect(effects));
            Assert.That(calls, Is.Zero);
        }

        private static IBonusEffect Record(string name, List<string> calls, List<BonusContext> contexts)
        {
            return new RecordingEffect(context =>
            {
                calls.Add(name);
                contexts.Add(context);
            });
        }

        private sealed class RecordingEffect : IBonusEffect
        {
            private readonly Action<BonusContext> _apply;

            public RecordingEffect(Action<BonusContext> apply)
            {
                _apply = apply;
            }

            public void Apply(BonusContext context)
            {
                _apply(context);
            }
        }
    }
}
