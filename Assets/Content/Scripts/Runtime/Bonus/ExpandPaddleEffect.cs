using Arkanoid.Paddle;
using VContainer;

namespace Arkanoid.Bonus
{
    public sealed class ExpandPaddleEffect : BonusEffect
    {
        private PaddleMovement _paddleMovement;
        private PaddleConfig _paddleConfig;

        [Inject]
        public void Construct(PaddleMovement paddle, PaddleConfig config)
        {
            _paddleMovement = paddle;
            _paddleConfig = config;
        }

        public override void Apply()
        {
            _paddleMovement.SetWidth(_paddleConfig.Width * 1.5f);
        }
    }
}
