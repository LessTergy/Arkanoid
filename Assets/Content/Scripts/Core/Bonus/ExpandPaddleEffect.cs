namespace Arkanoid.Core.Bonus
{
    public sealed class ExpandPaddleEffect : IBonusEffect
    {
        public void Apply(BonusContext context)
        {
            context.ExpandPaddle();
        }
    }
}
