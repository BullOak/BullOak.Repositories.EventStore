using BenchmarkDotNet.Attributes;

namespace Benchmark;

// base class to ensure that parameters values are in sync across Read and Write benchmarks
public class BenchmarkParameters
{
    [Params(10, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000)] public int EventsCount;
    [Params(10, 20, 50)] public int EventSize;
}
