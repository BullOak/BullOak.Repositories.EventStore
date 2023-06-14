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
            ("3.0.0-alpha-024", true),
            ("3.0.0-alpha-023", false),
            ("3.0.0-alpha-021", false)
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
