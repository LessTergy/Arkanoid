using Arkanoid.Core.GameFlow;
using VContainer;

namespace Arkanoid.Bonus
{
    public sealed class AddLifeEffect : BonusEffect
    {
        private LivesModel _livesModel;

        [Inject]
        public void Construct(LivesModel lives)
        {
            _livesModel = lives;
        }

        public override void Apply()
        {
            _livesModel.TryAddLife();
        }
    }
}
