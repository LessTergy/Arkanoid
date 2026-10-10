using System;
using System.Collections.Generic;
using System.Reflection;
using Arkanoid.Bonus;
using Arkanoid.Bricks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusDropDefinitionTests
    {
        private BonusDropDefinition _drop;
        private BonusDefinition _bonus;
        private ExpandPaddleEffectDefinition _effect;
        private BrickDefinition _brick;
        private BonusPickup _pickup;
        private Rigidbody2D _body;
        private BoxCollider2D _collider;

        [SetUp]
        public void SetUp()
        {
            _drop = ScriptableObject.CreateInstance<BonusDropDefinition>();
            _brick = ScriptableObject.CreateInstance<BrickDefinition>();
            var pickupObject = new GameObject("Pickup reference");
            _body = pickupObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _collider = pickupObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _pickup = pickupObject.AddComponent<BonusPickup>();
            SetField(_pickup, "_rigidbody", _body);
            SetField(_pickup, "_collider", _collider);
            _bonus = ScriptableObject.CreateInstance<BonusDefinition>();
            _effect = ScriptableObject.CreateInstance<ExpandPaddleEffectDefinition>();
            SetField(_bonus, "_effect", _effect);
            SetField(_bonus, "_pickupPrefab", _pickup);
            SetEntries(Entry(_bonus));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_body.gameObject);
            Object.DestroyImmediate(_brick);
            Object.DestroyImmediate(_drop);
            Object.DestroyImmediate(_bonus);
            Object.DestroyImmediate(_effect);
        }

        [Test]
        public void Brick_WithoutProfile_HasNoDropAndPreservesExistingRules()
        {
            Assert.That(_brick.BonusDrop, Is.Null);
            Assert.That(_brick.CreateDropSettings(), Is.Null);
            Assert.That(_brick.CreateSettings().BaseScore, Is.EqualTo(100));
        }

        [Test]
        public void AssignedProfile_CreatesIndependentNumericSnapshotAndKeepsPrefabInRuntime()
        {
            SetField(_bonus, "_pickupPrefab", _pickup);
            SetField(_brick, "_bonusDrop", _drop);

            var snapshot = _brick.CreateDropSettings();

            Assert.That(snapshot.Chance, Is.EqualTo(0.25f));
            Assert.That(_brick.BonusDrop, Is.SameAs(_drop));
            Assert.That(_bonus.PickupPrefab, Is.SameAs(_pickup));
            Assert.That(_brick.CreateSettings().MaxHealth, Is.EqualTo(1));

            SetField(_drop, "_chance", 1f);

            Assert.That(snapshot.Chance, Is.EqualTo(0.25f));
            Assert.That(_brick.CreateDropSettings().Chance, Is.EqualTo(1f));
            Assert.That(_bonus.PickupPrefab, Is.SameAs(_pickup));
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(1f)]
        public void AssignedProfile_WithoutPrefab_IsRejectedEvenAtZeroChance(float chance)
        {
            SetField(_bonus, "_pickupPrefab", null);
            SetField(_drop, "_chance", chance);
            SetField(_brick, "_bonusDrop", _drop);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
            Assert.Throws<InvalidOperationException>(() => _brick.CreateDropSettings());
            Assert.Throws<InvalidOperationException>(() => _brick.CreateSettings());
            Assert.That(_bonus.PickupPrefab, Is.Null);
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void AssignedProfile_InvalidChance_IsRejectedWithoutRepairingAsset(float chance)
        {
            SetField(_drop, "_chance", chance);
            SetField(_bonus, "_pickupPrefab", _pickup);
            SetField(_brick, "_bonusDrop", _drop);

            Assert.Throws<ArgumentOutOfRangeException>(() => _drop.CreateSettings());
            Assert.Throws<ArgumentOutOfRangeException>(() => _brick.CreateDropSettings());
            Assert.Throws<ArgumentOutOfRangeException>(() => _brick.CreateSettings());
            Assert.That(_bonus.PickupPrefab, Is.SameAs(_pickup));
            Assert.That(GetField(_drop, "_chance"), Is.EqualTo(chance));
        }

        [Test]
        public void AssignedProfile_DestroyedPickupReference_IsRejected()
        {
            SetField(_bonus, "_pickupPrefab", _pickup);
            Object.DestroyImmediate(_pickup);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [TestCase(false, RigidbodyType2D.Kinematic)]
        [TestCase(true, RigidbodyType2D.Dynamic)]
        public void AssignedProfile_InvalidPickupPhysics_IsRejected(bool isTrigger, RigidbodyType2D bodyType)
        {
            _collider.isTrigger = isTrigger;
            _body.bodyType = bodyType;
            SetField(_bonus, "_pickupPrefab", _pickup);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [Test]
        public void AssignedProfile_InactivePrefab_IsRejectedWithoutActivatingIt()
        {
            _pickup.gameObject.SetActive(false);
            SetField(_bonus, "_pickupPrefab", _pickup);
            SetField(_brick, "_bonusDrop", _drop);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
            Assert.Throws<InvalidOperationException>(() => _brick.CreateSettings());
            Assert.That(_pickup.gameObject.activeSelf, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void AssignedProfile_InvalidFallSpeed_IsRejected(float speed)
        {
            SetField(_pickup, "_fallSpeed", speed);
            SetField(_bonus, "_pickupPrefab", _pickup);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [TestCase("_rigidbody")]
        [TestCase("_collider")]
        public void AssignedProfile_MissingPhysicsReference_IsRejected(string field)
        {
            SetField(_pickup, field, null);
            SetField(_bonus, "_pickupPrefab", _pickup);

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [TestCase("_rigidbody")]
        [TestCase("_collider")]
        public void AssignedProfile_PhysicsReferenceFromAnotherObject_IsRejected(string field)
        {
            var otherObject = new GameObject("Other pickup physics");
            try
            {
                var body = otherObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                var collider = otherObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                SetField(_pickup, field, field == "_rigidbody" ? (Component)body : collider);
                SetField(_bonus, "_pickupPrefab", _pickup);

                Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
            }
            finally
            {
                Object.DestroyImmediate(otherObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyOrNullTable_IsRejected(bool nullTable)
        {
            SetField(_drop, "_entries", nullTable ? null : new List<BonusDropEntry>());

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullEntryOrBonus_IsRejectedAtZeroChance(bool nullEntry)
        {
            SetField(_drop, "_chance", 0f);
            SetEntries(Entry(_bonus), nullEntry ? null : Entry(null));

            Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidWeight_IsRejectedAtZeroChanceWithoutRepair(float weight)
        {
            SetField(_drop, "_chance", 0f);
            var entry = Entry(_bonus, weight);
            SetEntries(Entry(_bonus), entry);

            Assert.Throws<ArgumentOutOfRangeException>(() => _drop.CreateSettings());
            Assert.That(entry.Weight, Is.EqualTo(weight));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void UnselectedInvalidBonus_IsRejectedEvenAtZeroChance(int kind)
        {
            var bonus = ScriptableObject.CreateInstance<BonusDefinition>();
            var group = ScriptableObject.CreateInstance<CompositeBonusEffectDefinition>();
            try
            {
                SetField(bonus, "_pickupPrefab", _pickup);
                SetField(bonus, "_effect", _effect);
                switch (kind)
                {
                    case 0:
                        SetField(bonus, "_effect", null);
                        break;
                    case 1:
                        SetField(bonus, "_pickupPrefab", null);
                        break;
                    case 2:
                        SetField(bonus, "_effect", group);
                        break;
                    case 3:
                        SetField(group, "_effects", new List<BonusEffectDefinition> { group });
                        SetField(bonus, "_effect", group);
                        break;
                }

                SetField(_drop, "_chance", 0f);
                SetEntries(Entry(_bonus), Entry(bonus));

                Assert.Throws<InvalidOperationException>(() => _drop.CreateSettings());
                SetField(_brick, "_bonusDrop", _drop);
                Assert.Throws<InvalidOperationException>(() => _brick.CreateSettings());
            }
            finally
            {
                Object.DestroyImmediate(group);
                Object.DestroyImmediate(bonus);
            }
        }

        [Test]
        public void Settings_CopyEntryWeightsInOrder()
        {
            var entry = Entry(_bonus, 3f);
            SetEntries(Entry(_bonus), entry);
            var snapshot = _drop.CreateSettings();
            SetField(entry, "_weight", 10f);

            Assert.That(snapshot.Weights, Is.EqualTo(new[] { 1f, 3f }));
            Assert.That(_drop.CreateSettings().Weights, Is.EqualTo(new[] { 1f, 10f }));
        }

        private void SetEntries(params BonusDropEntry[] entries)
        {
            SetField(_drop, "_entries", new List<BonusDropEntry>(entries));
        }

        private static BonusDropEntry Entry(BonusDefinition bonus, float weight = 1f)
        {
            var entry = new BonusDropEntry();
            SetField(entry, "_bonus", bonus);
            SetField(entry, "_weight", weight);
            return entry;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
