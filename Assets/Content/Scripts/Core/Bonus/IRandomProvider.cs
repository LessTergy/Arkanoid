namespace Arkanoid.Core.Bonus
{
    public interface IRandomProvider
    {
        /// <returns>A finite value in the inclusive interval [0, 1].</returns>
        float NextFloat01();
    }
}
