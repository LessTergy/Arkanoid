using UnityEngine;

namespace Arkanoid.Bricks
{
    public enum BrickTypeId
    {
        Basic = 0,
    }

    public sealed class BrickView : MonoBehaviour
    {
        [SerializeField] private BrickTypeId _typeId;

        public BrickTypeId TypeId => _typeId;
    }
}
