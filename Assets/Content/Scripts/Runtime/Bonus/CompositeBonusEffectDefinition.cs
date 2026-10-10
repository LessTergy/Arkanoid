using System.Collections.Generic;
using UnityEngine;

namespace Arkanoid.Bonus
{
    [CreateAssetMenu(fileName = "CompositeBonusEffect", menuName = "Arkanoid/Bonuses/Effects/Composite")]
    public sealed class CompositeBonusEffectDefinition : BonusEffectDefinition
    {
        [SerializeField] private List<BonusEffectDefinition> _effects = new();

        public IReadOnlyList<BonusEffectDefinition> Effects => _effects;
    }
}
