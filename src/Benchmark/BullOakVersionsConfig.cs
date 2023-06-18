using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Benchmark;

public class BullOakVersionsConfig : ManualConfig
{
    public BullOakVersionsConfig()
    {
        WithOptions(ConfigOptions.Default);

        var baseJob = Job.Default;

        const string nugetPackage = "BullOak.Repositories.EventStore";
        var nugetPackageVersions = new[]
        {
            ("3.0.0", true),
            ("3.0.0-rc2", false),
            ("3.0.0-rc1", false)
        };

        foreach (var (version, isBaseline) in nugetPackageVersions)
        {
            AddJob(baseJob
                .WithNuGet(nugetPackage, version)
                .WithId(version)
                .WithBaseline(isBaseline));
        }
    }
}
