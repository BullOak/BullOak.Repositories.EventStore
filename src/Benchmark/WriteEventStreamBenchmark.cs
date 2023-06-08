using Benchmark.Model;
using BenchmarkDotNet.Attributes;
using BullOak.Repositories;
using BullOak.Repositories.Config;
using BullOak.Repositories.EventStore;
using BullOak.Repositories.EventStore.Streams;
using EventStore.Client;
using Perfolizer.Mathematics.OutlierDetection;

namespace Benchmark;

[MemoryDiagnoser]
public class WriteEventStreamBenchmark : BenchmarkParameters
{
    private readonly EventStoreRepository<string, IHoldTestState> repository;

    public WriteEventStreamBenchmark()
    {
        var settings = EventStoreClientSettings
            .Create("esdb://localhost:2114?tls=false");
        var client = new EventStoreClient(settings);

        var configuration = Configuration.Begin()
            .WithDefaultCollection()
            .WithDefaultStateFactory()
            .NeverUseThreadSafe()
            .WithNoEventPublisher()
            .WithAnyAppliersFrom(typeof(TestApplier).Assembly)
            .AndNoMoreAppliers()
            .WithNoUpconverters()
            .Build();

        var reader = new GrpcEventReader(client, configuration);
        var writer = new GrpcEventWriter(client);

        repository = new EventStoreRepository<string, IHoldTestState>(configuration, reader, writer);
    }

    [Benchmark]
    [WarmupCount(0)]
    [MinIterationCount(1)]
    [MaxIterationCount(2)]
    [Outliers(OutlierMode.DontRemove)]
    public async Task WriteStream()
    {
        // TODO: Use EventStore client directly, bypassing BullOak - currently it is taking very long time to complete

        var (streamId, events) = EventsGenerator.Generate(EventsCount, EventSize);

        using var writeSessionCheck = await repository.BeginSessionFor(streamId, throwIfNotExists: false);
        if (!writeSessionCheck.IsNewState) return;

        // workaround against "MaximumAppendSizeExceededException: Maximum Append Size of 1048576 Exceeded"
        foreach (var chunk in events.Chunk((int)Math.Ceiling(250 / (double)EventSize)))
        {
            using var writeSession = await repository.BeginSessionFor(streamId, throwIfNotExists: false);
            writeSession.AddEvents(chunk);
            await writeSession.SaveChanges();
        }
    }
}
