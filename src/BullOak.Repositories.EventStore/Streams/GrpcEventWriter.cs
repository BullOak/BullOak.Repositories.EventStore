namespace BullOak.Repositories.EventStore.Streams;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptions;
using global::EventStore.Client;
using EventData = global::EventStore.Client.EventData;

public class GrpcEventWriter : IStoreEventsToStream
{
    private readonly EventStoreClient client;

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
        IWriteResult writeResult;

        if (revision == -1)
        {
            writeResult = await client.AppendToStreamAsync(
                    streamId,
                    StreamState.NoStream,
                    eventsToAdd.Select(eventObject => eventObject.CreateV20EventData(dateTimeProvider)),
                    options => options.ThrowOnAppendFailure = false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            writeResult = await client.AppendToStreamAsync(
                    streamId,
                    StreamRevision.FromInt64(revision),
                    eventsToAdd.Select(eventObject => eventObject.CreateV20EventData(dateTimeProvider)),
                    options => options.ThrowOnAppendFailure = false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (writeResult is WrongExpectedVersionResult)
            throw new ConcurrencyException(streamId, null);

        return (int)writeResult.NextExpectedStreamRevision.ToInt64();
    }
}
