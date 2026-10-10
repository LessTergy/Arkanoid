using System;
using Arkanoid.Core.Bonus;
using Arkanoid.Levels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid.Bonus
{
    internal sealed class BonusFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly LevelView _level;
        private readonly BonusEffectFactory _effectFactory;

        public BonusFactory(IObjectResolver resolver, LevelView level, BonusEffectFactory effectFactory)
        {
            _resolver = resolver;
            _level = level;
            _effectFactory = effectFactory;
        }

        public BonusPickup Create(BonusDefinition definition, Vector3 position)
        {
            var effect = _effectFactory.Create(definition);
            return Create(definition.PickupPrefab, position, effect);
        }

        public BonusPickup Create(BonusPickup prefab, Vector3 position, IBonusEffect effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            var pickup = _resolver.Instantiate(prefab, position, Quaternion.identity, _level.transform);
            pickup.InitializeEffect(effect);
            return pickup;
        }
    }
}
