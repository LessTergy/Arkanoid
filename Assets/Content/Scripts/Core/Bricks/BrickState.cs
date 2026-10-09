namespace Arkanoid.Core.Bricks
{
    public sealed class BrickState
    {
        public BrickState(BrickSettings settings)
        {
            Settings = settings;
            CurrentHealth = settings.MaxHealth;
            CurrentShieldCharges = settings.ShieldCharges;
        }

        public BrickSettings Settings { get; }
        public int CurrentHealth { get; private set; }
        public int CurrentShieldCharges { get; private set; }

        public bool IsDestroyed
        {
            get
            {
                return CurrentHealth == 0;
            }
        }

        public bool TryConsumeShieldCharge()
        {
            if (IsDestroyed || Settings.IsIndestructible || CurrentShieldCharges == 0)
            {
                return false;
            }

            CurrentShieldCharges--;
            return true;
        }

        public bool TryApplyDamage()
        {
            if (IsDestroyed || Settings.IsIndestructible || CurrentShieldCharges > 0)
            {
                return false;
            }

            CurrentHealth--;
            return true;
        }
    }
}
