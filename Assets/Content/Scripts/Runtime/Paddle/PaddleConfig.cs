using UnityEngine;

namespace Arkanoid.Paddle
{
    [CreateAssetMenu(fileName = nameof(PaddleConfig), menuName = "Arkanoid/Paddle Config")]
    public sealed class PaddleConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _speed = 8f;
        [SerializeField, Min(0.01f)] private float _width = 2;

        public float Speed => _speed;
        public float Width => _width;
    }
}
