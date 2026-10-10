namespace Arkanoid.Core.Bonus
{
    public sealed class AddLifeEffect : IBonusEffect
    {
        public void Apply(BonusContext context)
        {
            context.Lives.TryAddLife();
        }
    }
}
