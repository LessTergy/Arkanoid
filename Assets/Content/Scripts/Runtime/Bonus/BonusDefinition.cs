using UnityEngine;

namespace Arkanoid.Bonus
{
    [CreateAssetMenu(fileName = nameof(BonusDefinition), menuName = "Arkanoid/Bonuses/Bonus Definition")]
    public sealed class BonusDefinition : ScriptableObject
    {
        [SerializeField] private BonusEffectDefinition _effect;
        [SerializeField] private BonusPickup _pickupPrefab;

        public BonusEffectDefinition Effect => _effect;

        public BonusPickup PickupPrefab => _pickupPrefab;
    }
}
