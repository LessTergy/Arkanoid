using System;
using System.Collections.Generic;

namespace Arkanoid.Core.Bonus
{
    public sealed class BonusDropSettings
    {
        public BonusDropSettings(float chance, IReadOnlyList<float> weights)
        {
            if (float.IsNaN(chance) || float.IsInfinity(chance) || chance < 0f || chance > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(chance), chance,
                    "Drop chance must be finite and between 0 and 1.");
            }

            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (weights.Count == 0)
            {
                throw new ArgumentException("Drop table must contain at least one weight.", nameof(weights));
            }

            var snapshot = new float[weights.Count];
            var total = 0d;
            for (var i = 0; i < snapshot.Length; i++)
            {
                var weight = weights[i];
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), weight,
                        "Drop weights must be finite and positive.");
                }

                snapshot[i] = weight;
                total += weight;
            }

            Chance = chance;
            Weights = Array.AsReadOnly(snapshot);
            TotalWeight = total;
        }

        public float Chance { get; }
        public IReadOnlyList<float> Weights { get; }
        public double TotalWeight { get; }
    }
}
