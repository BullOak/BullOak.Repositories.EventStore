namespace BullOak.Repositories.EventStore.Streams;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BullOak.Repositories.Exceptions;
using global::EventStore.Client;
using EventData = global::EventStore.Client.EventData;
using Polly;
using Polly.Retry;

public class GrpcEventWriter : IStoreEventsToStream
{
    private readonly EventStoreClient client;

    // TODO: Get this via .ctor dependency injection
    private readonly AsyncRetryPolicy retryPolicy =
        GrpcWriteRetryPolicy.GetRetryPolicy(GrpcWriteRetrySettings.DefaultRetrySettings);

    public GrpcEventWriter(EventStoreClient client)
    {
        this.client = client;
    }

    public async Task<int> Add
    (
        string streamId,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken = default
    ) => await AppendToStream(streamId, -1, eventsToAdd, dateTimeProvider, cancellationToken);


    public async Task<int> AppendTo
    (
        string streamId,
        long revision,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken = default
    ) => await AppendToStream(streamId, revision, eventsToAdd, dateTimeProvider, cancellationToken);

    public async Task SoftDelete(string streamId)
    {
        await client.DeleteAsync(streamId, StreamState.Any);
    }

    public async Task SoftDeleteByEvent<T>(string streamId, IEnumerable<T> eventData)
    {
        var events = eventData as EventData[] ?? eventData.Cast<EventData>().ToArray();
        await client.AppendToStreamAsync(streamId, StreamState.Any, events);
    }

    private async Task<int> AppendToStream
    (
        string streamId,
        long revision,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken
    )
    {
        var result = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            revision == -1 ?
                await client.AppendToStreamAsync(
                        streamId,
                        StreamState.NoStream,
                        eventsToAdd.Select(eventObject => eventObject.CreateV20EventData(dateTimeProvider)),
                        options => options.ThrowOnAppendFailure = false,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false)
            :
                await client.AppendToStreamAsync(
                        streamId,
                        StreamRevision.FromInt64(revision),
                        eventsToAdd.Select(eventObject => eventObject.CreateV20EventData(dateTimeProvider)),
                        options => options.ThrowOnAppendFailure = false,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false)
        );

        if (result.Outcome != OutcomeType.Successful)
        {
            // TODO: Use more specific exception
            throw new InvalidOperationException();
        }

        var writeResult = result.Result;

        if (writeResult is WrongExpectedVersionResult)
            throw new ConcurrencyException(streamId, null);

        return (int)writeResult.NextExpectedStreamRevision.ToInt64();
    }
}
