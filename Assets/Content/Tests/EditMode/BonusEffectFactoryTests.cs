using System;
using System.Collections.Generic;
using System.Reflection;
using Arkanoid.Bonus;
using Arkanoid.Core.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusEffectFactoryTests
    {
        private readonly List<Object> _objects = new();
        private BonusEffectFactory _factory;
        private LivesModel _lives;
        private DoubleScoreDecorator _doubleScore;
        private BonusContext _context;
        private BonusPickup _pickup;
        private int _expansionCalls;

        [SetUp]
        public void SetUp()
        {
            _factory = new BonusEffectFactory();
            _lives = new LivesModel();
            _doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator());
            _expansionCalls = 0;
            _context = new BonusContext(_lives, _doubleScore, () => _expansionCalls++);
            var pickupObject = new GameObject("Factory pickup template");
            _objects.Add(pickupObject);
            var body = pickupObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = pickupObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            _pickup = pickupObject.AddComponent<BonusPickup>();
            SetField(_pickup, "_rigidbody", body);
            SetField(_pickup, "_collider", collider);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in _objects)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            _objects.Clear();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LeafDefinition_BuildsCorrespondingBehavior(int index)
        {
            var definitions = new BonusEffectDefinition[]
            {
                NewAsset<ExpandPaddleEffectDefinition>(),
                NewAsset<AddLifeEffectDefinition>(),
                NewAsset<EnableDoubleScoreEffectDefinition>()
            };

            _factory.Create(definitions[index]).Apply(_context);

            Assert.That(_expansionCalls, Is.EqualTo(index == 0 ? 1 : 0));
            Assert.That(_lives.RemainingLives, Is.EqualTo(index == 1 ? 4 : 3));
            Assert.That(_doubleScore.IsEnabled, Is.EqualTo(index == 2));
        }

        [Test]
        public void Group_AppliesExpansionBeforeAddingLife()
        {
            var group = Group(NewAsset<ExpandPaddleEffectDefinition>(), NewAsset<AddLifeEffectDefinition>());
            var calls = new List<string>();
            var context = new BonusContext(_lives, _doubleScore, () =>
            {
                Assert.That(_lives.RemainingLives, Is.EqualTo(3));
                calls.Add("expand");
            });
            _lives.LivesChanged += _ => calls.Add("life");

            _factory.Create(group).Apply(context);

            Assert.That(calls, Is.EqualTo(new[] { "expand", "life" }));
            Assert.That(_lives.RemainingLives, Is.EqualTo(4));
            Assert.That(_doubleScore.IsEnabled, Is.False);
        }

        [Test]
        public void NestedGroup_ReferencesChildGroupAndEnablesDoubleAfterItsChildren()
        {
            var expand = NewAsset<ExpandPaddleEffectDefinition>();
            var life = NewAsset<AddLifeEffectDefinition>();
            var childGroup = Group(expand, life);
            var doubleScore = NewAsset<EnableDoubleScoreEffectDefinition>();
            var parentGroup = Group(childGroup, doubleScore);
            var calls = new List<string>();
            var context = new BonusContext(_lives, _doubleScore, () =>
            {
                Assert.That(_doubleScore.IsEnabled, Is.False);
                calls.Add("expand");
            });
            _lives.LivesChanged += _ =>
            {
                Assert.That(_doubleScore.IsEnabled, Is.False);
                calls.Add("life");
            };

            _factory.Create(parentGroup).Apply(context);

            Assert.That(parentGroup.Effects[0], Is.SameAs(childGroup));
            Assert.That(childGroup.Effects, Is.EqualTo(new BonusEffectDefinition[] { expand, life }));
            Assert.That(calls, Is.EqualTo(new[] { "expand", "life" }));
            Assert.That(_lives.RemainingLives, Is.EqualTo(4));
            Assert.That(_doubleScore.IsEnabled, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedDefinitionAcrossBranches_IsAllowedAndAppliedForEachEntry(bool reuseGroup)
        {
            BonusEffectDefinition child = NewAsset<AddLifeEffectDefinition>();
            if (reuseGroup)
            {
                child = Group(child);
            }

            _factory.Create(Group(child, child)).Apply(_context);

            Assert.That(_lives.RemainingLives, Is.EqualTo(5));
        }

        [Test]
        public void BuiltEffect_DoesNotReadLaterDefinitionChanges()
        {
            var composite = Group(NewAsset<AddLifeEffectDefinition>());
            var effect = _factory.Create(composite);
            SetField(composite, "_effects", new List<BonusEffectDefinition>());

            effect.Apply(_context);

            Assert.That(_lives.RemainingLives, Is.EqualTo(4));
        }

        [Test]
        public void MissingRootEffect_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.Create((BonusEffectDefinition)null));
        }

        [Test]
        public void DestroyedEffectReference_IsRejected()
        {
            var effect = NewAsset<AddLifeEffectDefinition>();
            Object.DestroyImmediate(effect);

            Assert.Throws<InvalidOperationException>(() => _factory.Create(effect));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyOrNullGroup_IsRejected(bool nullList)
        {
            var composite = Group();
            if (nullList)
            {
                SetField(composite, "_effects", null);
            }

            Assert.Throws<InvalidOperationException>(() => _factory.Create(composite));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void MissingChild_IsRejectedWithoutApplyingEarlierEffects(int index)
        {
            var children = new BonusEffectDefinition[]
            {
                NewAsset<ExpandPaddleEffectDefinition>(), NewAsset<AddLifeEffectDefinition>()
            };
            children[index] = null;

            Assert.Throws<InvalidOperationException>(() => _factory.Create(Group(children)));
            Assert.That(_expansionCalls, Is.Zero);
            Assert.That(_lives.RemainingLives, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DirectIndirectAndNestedCycles_AreRejected(int kind)
        {
            var first = Group();
            var second = Group();
            SetField(first, "_effects", new List<BonusEffectDefinition> { kind == 0 ? first : second });
            SetField(second, "_effects", new List<BonusEffectDefinition> { kind == 1 ? first : second });

            Assert.Throws<InvalidOperationException>(() => _factory.Create(first));
            Assert.That(_lives.RemainingLives, Is.EqualTo(3));
            Assert.That(_expansionCalls, Is.Zero);
        }

        [Test]
        public void UnsupportedDefinition_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.Create(NewAsset<UnsupportedDefinition>()));
        }

        [Test]
        public void BonusDefinition_BuildsEffectWithoutChangingData()
        {
            var effect = NewAsset<AddLifeEffectDefinition>();
            var bonus = Bonus(effect);

            _factory.Create(bonus).Apply(_context);

            Assert.That(bonus.Effect, Is.SameAs(effect));
            Assert.That(bonus.PickupPrefab, Is.SameAs(_pickup));
            Assert.That(_pickup.gameObject.activeSelf, Is.True);
            Assert.That(_lives.RemainingLives, Is.EqualTo(4));
        }

        [Test]
        public void MissingBonusDefinition_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _factory.Create((BonusDefinition)null));
        }

        [Test]
        public void BonusDefinition_WithoutPrefabIsRejected()
        {
            var bonus = Bonus(NewAsset<AddLifeEffectDefinition>());
            SetField(bonus, "_pickupPrefab", null);

            Assert.Throws<InvalidOperationException>(() => _factory.Create(bonus));
        }

        [Test]
        public void BonusDefinition_WithoutEffectIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.Create(Bonus(null)));
        }

        [Test]
        public void BonusDefinition_InactivePrefabIsRejected()
        {
            _pickup.gameObject.SetActive(false);

            Assert.Throws<InvalidOperationException>(() => _factory.Create(Bonus(NewAsset<AddLifeEffectDefinition>())));
        }

        [Test]
        public void BonusDefinition_InvalidPhysicsIsRejected()
        {
            _pickup.GetComponent<BoxCollider2D>().isTrigger = false;

            Assert.Throws<InvalidOperationException>(() => _factory.Create(Bonus(NewAsset<AddLifeEffectDefinition>())));
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private CompositeBonusEffectDefinition Group(params BonusEffectDefinition[] effects)
        {
            var group = NewAsset<CompositeBonusEffectDefinition>();
            SetField(group, "_effects", new List<BonusEffectDefinition>(effects));
            return group;
        }

        private BonusDefinition Bonus(BonusEffectDefinition effect)
        {
            var bonus = NewAsset<BonusDefinition>();
            SetField(bonus, "_effect", effect);
            SetField(bonus, "_pickupPrefab", _pickup);
            return bonus;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class UnsupportedDefinition : BonusEffectDefinition
        {
        }
    }
}
