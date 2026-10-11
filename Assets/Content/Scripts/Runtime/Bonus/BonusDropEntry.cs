using System;
using UnityEngine;

namespace Arkanoid.Bonus
{
    [Serializable]
    public sealed class BonusDropEntry
    {
        [SerializeField] private BonusPickup _pickupPrefab;
        [SerializeField] private float _weight = 1f;

        public BonusPickup PickupPrefab => _pickupPrefab;

        public float Weight => _weight;
    }
}
