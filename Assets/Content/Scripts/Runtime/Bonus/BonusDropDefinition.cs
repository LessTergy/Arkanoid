using System;
using System.Collections.Generic;
using Arkanoid.Core.Bonus;
using UnityEngine;

namespace Arkanoid.Bonus
{
    [CreateAssetMenu(fileName = nameof(BonusDropDefinition), menuName = "Arkanoid/Bonus Drop Definition")]
    public sealed class BonusDropDefinition : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float _chance = 0.25f;
        [SerializeField] private List<BonusDropEntry> _entries = new();

        public IReadOnlyList<BonusDropEntry> Entries => _entries;

        public BonusDropSettings CreateSettings()
        {
            if (_entries == null || _entries.Count == 0)
            {
                throw new InvalidOperationException($"Bonus drop definition '{name}' requires a nonempty drop table.");
            }

            var weights = new float[_entries.Count];
            for (var i = 0; i < weights.Length; i++)
            {
                if (_entries[i] == null || _entries[i].PickupPrefab == null)
                {
                    throw new InvalidOperationException($"Bonus drop definition '{name}' has an unassigned entry at {i}.");
                }

                weights[i] = _entries[i].Weight;
            }

            var settings = new BonusDropSettings(_chance, weights);
            foreach (var entry in _entries)
            {
                entry.PickupPrefab.ValidatePrefabConfiguration();
            }

            return settings;
        }
    }
}
