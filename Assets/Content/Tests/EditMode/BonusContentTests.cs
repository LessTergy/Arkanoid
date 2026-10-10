using Arkanoid.Bonus;
using Arkanoid.Core.Bonus;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using NUnit.Framework;
using UnityEditor;

namespace Arkanoid.Tests.EditMode
{
    public sealed class BonusContentTests
    {
        [TestCase("ExpandPaddleEffect", 3, 3, false)]
        [TestCase("AddLifeEffect", 5, 0, false)]
        [TestCase("EnableDoubleScoreEffect", 3, 0, true)]
        public void SavedEffects_ApplyOnlyTheirOwnBehavior(
            string name, int expectedLives, int expectedExpansionCalls, bool enablesDouble)
        {
            var lives = new LivesModel();
            var doubleScore = new DoubleScoreDecorator(new BaseScoreCalculator());
            var expansionCalls = 0;
            var context = new BonusContext(lives, doubleScore, () => expansionCalls++);
            var effect = new BonusEffectFactory().Create(Load<BonusEffectDefinition>(name));

            effect.Apply(context);
            effect.Apply(context);
            doubleScore.IsEnabled = false;
            effect.Apply(context);

            Assert.That(lives.RemainingLives, Is.EqualTo(expectedLives));
            Assert.That(expansionCalls, Is.EqualTo(expectedExpansionCalls));
            Assert.That(doubleScore.IsEnabled, Is.EqualTo(enablesDouble));
        }

        [TestCase("ExpandPaddleBonus", "ExpandPaddleEffect")]
        [TestCase("AddLifeBonus", "AddLifeEffect")]
        [TestCase("DoubleScoreBonus", "EnableDoubleScoreEffect")]
        public void SavedBonusDefinition_ReferencesExpectedRoot(string bonusName, string effectName)
        {
            var bonus = Load<BonusDefinition>(bonusName);

            Assert.That(bonus.Effect, Is.SameAs(Load<BonusEffectDefinition>(effectName)));
            new BonusEffectFactory().Create(bonus);
        }

        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var folder = typeof(T) == typeof(BonusDefinition) ? "Definitions" : "Effects";
            var path = $"Assets/Content/Data/Bonuses/{folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"Expected imported {typeof(T).Name} at '{path}'.");
            return asset;
        }
    }
}
