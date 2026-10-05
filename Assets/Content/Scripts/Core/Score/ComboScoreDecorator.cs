namespace Arkanoid.Core.Score
{
    public sealed class ComboScoreDecorator : IScoreCalculator
    {
        private readonly IScoreCalculator _inner;
        private readonly ComboModel _comboModel;

        public ComboScoreDecorator(IScoreCalculator inner, ComboModel comboModel)
        {
            _inner = inner;
            _comboModel = comboModel;
        }

        public int Calculate(int baseScore)
        {
            var score = _inner.Calculate(baseScore);
            return checked(score * _comboModel.Multiplier);
        }
    }
}
