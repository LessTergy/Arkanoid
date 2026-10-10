using System;
using UnityEngine;

namespace Arkanoid.Bonus
{
    [Serializable]
    public sealed class BonusDropEntry
    {
        [SerializeField] private BonusDefinition _bonus;
        [SerializeField] private float _weight = 1f;

        public BonusDefinition Bonus => _bonus;

        public float Weight => _weight;
    }
}
