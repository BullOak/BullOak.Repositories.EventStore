using Benchmark.Model;
using BenchmarkDotNet.Attributes;
using BullOak.Repositories;
using BullOak.Repositories.Config;
using BullOak.Repositories.EventStore;
using BullOak.Repositories.EventStore.Streams;
using EventStore.Client;

namespace Benchmark;

[MemoryDiagnoser]
[RPlotExporter]
public class ReadEventStreamBenchmark : BenchmarkParameters
{
    private readonly EventStoreRepository<string, IHoldTestState> repository;

    public ReadEventStreamBenchmark()
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

    // This test expects data available in ESDB
    // Run WriteEventStreamBenchmark before running this one
    [Benchmark]
    [WarmupCount(1)]
    [MinIterationCount(5)]
    [MaxIterationCount(10)]
    public async Task LoadStream()
    {
        var (streamId, events) = EventsGenerator.Generate(EventsCount, EventSize);

        using var readSession = await repository.BeginSessionFor(streamId, throwIfNotExists: true);
        var state = readSession.GetCurrentState();
        if (state.Elements?.Length != EventSize)
            throw new InvalidOperationException(
                $"Expected elements count {EventSize}, got {state.Elements?.Length}");

        // Workaround against
        // Grpc.Core.RpcException: Status(
        //   StatusCode="ResourceExhausted",
        //   Detail="Error starting gRPC call.
        //     HttpRequestException: An error occurred while sending the request.
        //     IOException: The request was aborted.
        //     Http2StreamException: The HTTP/2 server reset the stream. HTTP/2 error code 'ENHANCE_YOUR_CALM' (0xb).",
        //   DebugException="System.Net.Http.HttpRequestException: An error occurred while sending the request.")
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
