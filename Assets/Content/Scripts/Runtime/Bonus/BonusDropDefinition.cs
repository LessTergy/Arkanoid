using System;
using Arkanoid.Core.Bonus;
using UnityEngine;

namespace Arkanoid.Bonus
{
    [CreateAssetMenu(fileName = nameof(BonusDropDefinition), menuName = "Arkanoid/Bonus Drop Definition")]
    public sealed class BonusDropDefinition : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float _chance = 0.25f;
        [SerializeField] private BonusPickup _pickupPrefab;

        public BonusPickup PickupPrefab => _pickupPrefab;

        public BonusDropSettings CreateSettings()
        {
            var settings = new BonusDropSettings(_chance);
            if (_pickupPrefab == null)
            {
                throw new InvalidOperationException($"Bonus drop definition '{name}' requires a pickup prefab.");
            }

            if (!_pickupPrefab.gameObject.activeSelf)
            {
                throw new InvalidOperationException($"Bonus drop definition '{name}' requires an active pickup prefab.");
            }

            _pickupPrefab.ValidateConfiguration();

            return settings;
        }
    }
}
