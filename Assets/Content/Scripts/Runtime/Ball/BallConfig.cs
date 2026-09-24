using UnityEngine;

namespace Arkanoid.Ball
{
    [CreateAssetMenu(fileName = nameof(BallConfig), menuName = "Arkanoid/Ball Config")]
    public sealed class BallConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _speed = 8f;
        [SerializeField, Range(0f, 89f)] private float _minimumLaunchAngleDegrees = 15f;
        [SerializeField, Range(0f, 89f)] private float _maximumLaunchAngleDegrees = 45f;
        [SerializeField, Range(0.01f, 1f)] private float _minimumVerticalComponent = 0.5f;

        public float Speed => _speed;
        public float MinimumLaunchAngleDegrees => _minimumLaunchAngleDegrees;
        public float MaximumLaunchAngleDegrees => _maximumLaunchAngleDegrees;
        public float MinimumVerticalComponent => _minimumVerticalComponent;
    }
}
