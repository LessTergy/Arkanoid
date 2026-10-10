namespace Arkanoid.Core.Bonus
{
    public sealed class EnableDoubleScoreEffect : IBonusEffect
    {
        public void Apply(BonusContext context)
        {
            context.DoubleScore.IsEnabled = true;
        }
    }
}
