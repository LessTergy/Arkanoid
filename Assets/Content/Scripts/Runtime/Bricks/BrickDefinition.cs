using Arkanoid.Core.Bricks;
using UnityEngine;

namespace Arkanoid.Bricks
{
    [CreateAssetMenu(fileName = nameof(BrickDefinition), menuName = "Arkanoid/Brick Definition")]
    public sealed class BrickDefinition : ScriptableObject
    {
        [Header("Rules")]
        [SerializeField, Min(0)] private int _baseScore = 100;
        [SerializeField, Range(BrickSettings.MinimumHealth, BrickSettings.MaximumHealth)] private int _maxHealth = 1;
        [SerializeField, Min(0)] private int _shieldCharges;
        [SerializeField] private bool _isIndestructible;

        [Header("Presentation")]
        [SerializeField] private Color _color = Color.white;

        public Color Color
        {
            get
            {
                return _color;
            }
        }

        public BrickSettings CreateSettings()
        {
            return new BrickSettings(_baseScore, _maxHealth, _shieldCharges, _isIndestructible);
        }
    }
}
