using System;
using Arkanoid.Bricks;
using Arkanoid.Core.Bricks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BrickDefinitionTests
    {
        private BrickDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<BrickDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void CreateSettings_DefaultDefinition_MatchesNormalPreset()
        {
            var settings = _definition.CreateSettings();

            Assert.That(settings.BaseScore, Is.EqualTo(100));
            Assert.That(settings.MaxHealth, Is.EqualTo(1));
            Assert.That(settings.ShieldCharges, Is.EqualTo(0));
            Assert.That(settings.IsIndestructible, Is.False);
            Assert.That(_definition.Color, Is.EqualTo(Color.white));
        }

        [Test]
        public void CreateSettings_SerializedData_CreatesIndependentSnapshot()
        {
            using (var serialized = new SerializedObject(_definition))
            {
                serialized.FindProperty("_baseScore").intValue = 200;
                serialized.FindProperty("_maxHealth").intValue = 3;
                serialized.FindProperty("_shieldCharges").intValue = 2;
                serialized.FindProperty("_isIndestructible").boolValue = true;
                serialized.FindProperty("_color").colorValue = Color.blue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var settings = _definition.CreateSettings();

            Assert.That(settings.BaseScore, Is.EqualTo(200));
            Assert.That(settings.MaxHealth, Is.EqualTo(3));
            Assert.That(settings.ShieldCharges, Is.EqualTo(2));
            Assert.That(settings.IsIndestructible, Is.True);
            Assert.That(_definition.Color, Is.EqualTo(Color.blue));

            using (var serialized = new SerializedObject(_definition))
            {
                serialized.FindProperty("_maxHealth").intValue = 5;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Assert.That(settings.MaxHealth, Is.EqualTo(3));
            Assert.That(_definition.CreateSettings().MaxHealth, Is.EqualTo(5));
        }

        [TestCase("_baseScore", -1, "baseScore")]
        [TestCase("_maxHealth", 0, "maxHealth")]
        [TestCase("_maxHealth", -1, "maxHealth")]
        [TestCase("_maxHealth", 6, "maxHealth")]
        [TestCase("_maxHealth", int.MaxValue, "maxHealth")]
        [TestCase("_shieldCharges", -1, "shieldCharges")]
        public void CreateSettings_InvalidSerializedData_ThrowsWithoutRepairingAsset(
            string fieldName, int value, string parameterName)
        {
            using (var serialized = new SerializedObject(_definition))
            {
                serialized.FindProperty(fieldName).intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _definition.CreateSettings());

            Assert.That(exception.ParamName, Is.EqualTo(parameterName));
            using (var serialized = new SerializedObject(_definition))
            {
                Assert.That(serialized.FindProperty(fieldName).intValue, Is.EqualTo(value));
            }
        }

        [Test]
        public void States_FromSameDefinition_DoNotChangeEachOtherOrDefinition()
        {
            using (var serialized = new SerializedObject(_definition))
            {
                serialized.FindProperty("_maxHealth").intValue = 3;
                serialized.FindProperty("_shieldCharges").intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var settings = _definition.CreateSettings();
            var first = new BrickState(settings);
            var second = new BrickState(settings);

            Assert.That(first.TryConsumeShieldCharge(), Is.True);
            Assert.That(first.TryApplyDamage(), Is.True);
            Assert.That(first.TryApplyDamage(), Is.True);
            Assert.That(first.TryApplyDamage(), Is.True);

            Assert.That(first.IsDestroyed, Is.True);
            Assert.That(second.CurrentHealth, Is.EqualTo(3));
            Assert.That(second.CurrentShieldCharges, Is.EqualTo(1));
            Assert.That(second.IsDestroyed, Is.False);

            var definitionAfterDamage = _definition.CreateSettings();
            Assert.That(definitionAfterDamage.BaseScore, Is.EqualTo(100));
            Assert.That(definitionAfterDamage.MaxHealth, Is.EqualTo(3));
            Assert.That(definitionAfterDamage.ShieldCharges, Is.EqualTo(1));
            Assert.That(definitionAfterDamage.IsIndestructible, Is.False);
            Assert.That(_definition.Color, Is.EqualTo(Color.white));
        }
    }
}
