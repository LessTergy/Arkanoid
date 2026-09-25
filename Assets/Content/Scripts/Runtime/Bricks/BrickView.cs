using System;
using Arkanoid.Ball;
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

        public event Action<BrickView> BrickDestroyed;

        private bool _destroyed;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_destroyed || !collision.gameObject.TryGetComponent<BallController>(out _))
            {
                return;
            }

            _destroyed = true;
            BrickDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
