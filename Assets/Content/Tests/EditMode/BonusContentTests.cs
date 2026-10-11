using System;
using System.Collections.Generic;
using System.Reflection;
using Arkanoid.Bonus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusContentTests
    {
        [TestCase("ExpandPaddlePickup", typeof(ExpandPaddleEffect))]
        [TestCase("AddLifePickup", typeof(AddLifeEffect))]
        [TestCase("DoubleScorePickup", typeof(EnableDoubleScoreEffect))]
        public void SavedPickup_HasOneExpectedRootEffectAndValidConfiguration(string name, Type effectType)
        {
            var prefab = LoadPrefab(name);
            var effects = prefab.GetComponents<BonusEffect>();
            Assert.That(effects.Length, Is.EqualTo(1));
            Assert.That(effects[0].GetType(), Is.EqualTo(effectType));
            Assert.That(effects[0].enabled, Is.True);
            var entry = new BonusDropEntry();
            SetField(entry, "_pickupPrefab", prefab);
            var profile = ScriptableObject.CreateInstance<BonusDropDefinition>();
            try
            {
                SetField(profile, "_entries", new List<BonusDropEntry> { entry });
                Assert.DoesNotThrow(() => profile.CreateSettings());
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SavedTemplate_HasNoInheritedGameplayEffect()
        {
            Assert.That(LoadPrefab("BonusPickup").GetComponents<BonusEffect>(), Is.Empty);
        }

        [Test]
        public void SavedDropProfile_ReferencesValidPickupPrefabsDirectly()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BonusDropDefinition>(
                "Assets/Content/Data/Bonuses/BonusDropDefinition.asset");
            Assert.That(profile, Is.Not.Null);
            Assert.DoesNotThrow(() => profile.CreateSettings());
            foreach (var entry in profile.Entries)
            {
                Assert.That(PrefabUtility.IsPartOfPrefabAsset(entry.PickupPrefab), Is.True);
            }
        }

        private static BonusPickup LoadPrefab(string name)
        {
            var path = $"Assets/Content/Prefabs/Gameplay/Bonuses/{name}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Expected imported prefab at '{path}'.");
            var pickup = prefab.GetComponent<BonusPickup>();
            Assert.That(pickup, Is.Not.Null);
            return pickup;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
