using Benchmark.Model;
using BenchmarkDotNet.Attributes;
using BullOak.Repositories;
using BullOak.Repositories.Config;
using BullOak.Repositories.EventStore;
using BullOak.Repositories.EventStore.Streams;
using EventStore.Client;

namespace Benchmark;

[MemoryDiagnoser]
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

    // This test expects data available in ESDB.
    // Run WriteEventStreamBenchmark to create event streams.
    [Benchmark]
    [WarmupCount(1)]
    [MinIterationCount(20)]
    [MaxIterationCount(50)]
    public async Task LoadStream()
    {
        var streamId = EventsGenerator.GetStreamId(EventsCount, EventSize);

        using var readSession = await repository.BeginSessionFor(streamId, throwIfNotExists: true);

        var state = readSession.GetCurrentState();
        if (state.Elements?.Length != EventSize)
            throw new InvalidOperationException(
                $"Expected elements count {EventSize}, got {state.Elements?.Length}");
    }

    [IterationSetup]
    public void WaitBeforeRead()
    {
        // TCP Connections limit workaround.
        //
        // Frequent GRPC requests may exhaust networking resources:
        //
        //     Grpc.Core.RpcException: Status(
        //       StatusCode="ResourceExhausted",
        //       Detail="Error starting gRPC call.
        //         HttpRequestException: An error occurred while sending the request.
        //         IOException: The request was aborted.
        //         Http2StreamException: The HTTP/2 server reset the stream. HTTP/2 error code 'ENHANCE_YOUR_CALM' (0xb).",
        //       DebugException="System.Net.Http.HttpRequestException: An error occurred while sending the request.")
        //
        // it means that OS TCP connection pool is exhausted.
        //
        // Increasing those TCP limits (see <https://stackoverflow.com/a/3923785>) helps to some extent,
        // but ResourceExhausted can still happen.
        //
        // Explicit delay seems to be a more reliable workaround.

        Thread.Sleep(TimeSpan.FromSeconds(1));
    }
}
