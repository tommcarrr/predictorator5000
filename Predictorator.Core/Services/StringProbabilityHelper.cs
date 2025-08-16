using System;

namespace Predictorator.Core.Services;

public static class StringProbabilityHelper
{
    public static string Choose(string first, string second, double probability, Random? random = null)
    {
        if (probability <= 0) return first;
        if (probability >= 1) return second;

        var rng = random ?? Random.Shared;
        return rng.NextDouble() < probability ? second : first;
    }
}
