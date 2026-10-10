using Arkanoid.Core.Bonus;
using UnityEngine;

namespace Arkanoid.Bonus
{
    public sealed class UnityRandomProvider : IRandomProvider
    {
        public float NextFloat01()
        {
            return Random.value;
        }
    }
}
