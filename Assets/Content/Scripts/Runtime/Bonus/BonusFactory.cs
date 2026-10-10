using Arkanoid.Levels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid.Bonus
{
    internal sealed class BonusFactory
    {
        private readonly IObjectResolver _resolver;
        private readonly LevelView _level;

        public BonusFactory(IObjectResolver resolver, LevelView level)
        {
            _resolver = resolver;
            _level = level;
        }

        public BonusPickup Create(BonusPickup prefab, Vector3 position)
        {
            return _resolver.Instantiate(prefab, position, Quaternion.identity, _level.transform);
        }
    }
}
