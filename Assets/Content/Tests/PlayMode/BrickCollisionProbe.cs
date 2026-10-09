using Arkanoid.Ball;
using UnityEngine;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class BrickCollisionProbe : MonoBehaviour
    {
        public int CollisionCount { get; private set; }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent<BallController>(out _))
            {
                CollisionCount++;
            }
        }
    }
}
