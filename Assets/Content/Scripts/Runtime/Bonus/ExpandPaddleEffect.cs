using Arkanoid.Paddle;

namespace Arkanoid.Bonus
{
    internal sealed class ExpandPaddleEffect
    {
        private readonly PaddleMovement _paddle;
        private readonly PaddleConfig _config;

        public ExpandPaddleEffect(PaddleMovement paddle, PaddleConfig config)
        {
            _paddle = paddle;
            _config = config;
        }

        public void Apply()
        {
            _paddle.SetWidth(_config.Width * 1.5f);
        }

        public void Reset()
        {
            _paddle.SetWidth(_config.Width);
        }
    }
}
