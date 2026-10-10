using System;
using System.Collections.Generic;
using Arkanoid.Core.Bonus;

namespace Arkanoid.Bonus
{
    public sealed class BonusEffectFactory
    {
        public IBonusEffect Create(BonusDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.PickupPrefab == null)
            {
                throw new InvalidOperationException($"Bonus definition '{definition.name}' requires a pickup prefab.");
            }

            definition.PickupPrefab.ValidatePrefabConfiguration();
            return Create(definition.Effect);
        }

        public IBonusEffect Create(BonusEffectDefinition definition)
        {
            return Create(definition, new HashSet<BonusEffectDefinition>());
        }

        private IBonusEffect Create(BonusEffectDefinition definition, HashSet<BonusEffectDefinition> path)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Bonus effect definition must be assigned.");
            }

            if (!path.Add(definition))
            {
                throw new InvalidOperationException($"Bonus effect graph contains a cycle at '{definition.name}'.");
            }

            try
            {
                switch (definition)
                {
                    case ExpandPaddleEffectDefinition:
                        return new ExpandPaddleEffect();
                    case AddLifeEffectDefinition:
                        return new AddLifeEffect();
                    case EnableDoubleScoreEffectDefinition:
                        return new EnableDoubleScoreEffect();
                    case CompositeBonusEffectDefinition composite:
                        if (composite.Effects == null || composite.Effects.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"Composite bonus definition '{definition.name}' requires at least one effect.");
                        }

                        var effects = new IBonusEffect[composite.Effects.Count];
                        for (var i = 0; i < effects.Length; i++)
                        {
                            effects[i] = Create(composite.Effects[i], path);
                        }

                        return new CompositeBonusEffect(effects);
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported bonus effect definition '{definition.name}' ({definition.GetType().Name}).");
                }
            }
            finally
            {
                path.Remove(definition);
            }
        }
    }
}
