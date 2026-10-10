using System;
using System.Collections.Generic;
using Arkanoid.Core.Bonus;

namespace Arkanoid.Tests.EditMode
{
    internal sealed class FakeRandomProvider : IRandomProvider
    {
        private readonly Queue<float> _values;

        public FakeRandomProvider(params float[] values)
        {
            _values = new Queue<float>(values);
        }

        public int Calls { get; private set; }

        public float NextFloat01()
        {
            Calls++;
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("No random values remain in the test sequence.");
            }

            return _values.Dequeue();
        }
    }
}
