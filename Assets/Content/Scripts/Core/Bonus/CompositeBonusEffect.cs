using System;
using System.Collections.Generic;

namespace Arkanoid.Core.Bonus
{
    public sealed class CompositeBonusEffect : IBonusEffect
    {
        private readonly IReadOnlyList<IBonusEffect> _effects;

        public CompositeBonusEffect(IReadOnlyList<IBonusEffect> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effects.Count == 0)
            {
                throw new ArgumentException("Composite bonus requires at least one effect.", nameof(effects));
            }

            var snapshot = new IBonusEffect[effects.Count];
            for (var i = 0; i < effects.Count; i++)
            {
                snapshot[i] = effects[i] ?? throw new ArgumentException(
                    $"Composite bonus effect at index {i} cannot be null.", nameof(effects));
            }

            _effects = snapshot;
        }

        public void Apply(BonusContext context)
        {
            foreach (var effect in _effects)
            {
                effect.Apply(context);
            }
        }
    }
}
