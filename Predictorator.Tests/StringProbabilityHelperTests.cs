using System.Collections.Generic;
using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class StringProbabilityHelperTests
{
    private class FixedRandom : Random
    {
        private readonly Queue<double> _values;
        public FixedRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public override double NextDouble()
        {
            return _values.Dequeue();
        }
    }

    [Fact]
    public void Choose_ReturnsSecond_When_Random_Less_Than_Probability()
    {
        var rng = new FixedRandom(0.1);
        var result = StringProbabilityHelper.Choose("a", "b", 0.2, rng);
        Assert.Equal("b", result);
    }

    [Fact]
    public void Choose_ReturnsFirst_When_Random_Greater_Than_Probability()
    {
        var rng = new FixedRandom(0.5);
        var result = StringProbabilityHelper.Choose("a", "b", 0.2, rng);
        Assert.Equal("a", result);
    }
}
