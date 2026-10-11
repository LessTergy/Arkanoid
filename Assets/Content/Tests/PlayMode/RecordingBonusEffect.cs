using Arkanoid.Bonus;
using Arkanoid.Core.GameFlow;
using VContainer;

namespace Arkanoid.Tests.PlayMode
{
    public sealed class RecordingBonusEffect : BonusEffect
    {
        private LivesModel _lives;

        public int ApplyCount { get; private set; }

        [Inject]
        public void Construct(LivesModel lives)
        {
            _lives = lives;
        }

        public override void Apply()
        {
            ApplyCount++;
            _lives.TryAddLife();
        }
    }
}
