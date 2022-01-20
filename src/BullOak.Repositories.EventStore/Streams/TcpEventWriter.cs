namespace BullOak.Repositories.EventStore.Streams;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptions;
using global::EventStore.ClientAPI;

public class TcpEventWriter : IStoreEventsToStream
{
    private readonly IEventStoreConnection connection;

    public TcpEventWriter(IEventStoreConnection connection)
    {
        this.connection = connection;
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
        var expectedVersion = await GetLastEventNumber(streamId);
        await connection.DeleteStreamAsync(streamId, expectedVersion);
    }

    public async Task SoftDeleteByEvent<T>(string streamId, IEnumerable<T> eventData)
    {
        var events = eventData as EventData[] ?? eventData.Cast<EventData>().ToArray();

        var expectedVersion = await GetLastEventNumber(streamId);
        var writeResult = await connection.ConditionalAppendToStreamAsync
            (
                streamId,
                expectedVersion,
                events
            )
            .ConfigureAwait(false);

        CheckConditionalWriteResultStatus(writeResult, streamId);
    }

    private async Task<long> GetLastEventNumber(string id)
    {
        var eventsTail = await GetLastEvent(id);
        return eventsTail.LastEventNumber;
    }

    private Task<StreamEventsSlice> GetLastEvent(string id)
        => connection.ReadStreamEventsBackwardAsync(id, StreamPosition.End, 1, false);

    private async Task<int> AppendToStream
    (
        string streamId,
        long revision,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken
    )
    {
        var writeResult = await connection.ConditionalAppendToStreamAsync
            (
                streamId,
                revision,
                eventsToAdd.Select(eventObject => eventObject.CreateV5EventData(dateTimeProvider))
            )
            .ConfigureAwait(false);

        CheckConditionalWriteResultStatus(writeResult, streamId);

        if (!writeResult.NextExpectedVersion.HasValue)
        {
            throw new InvalidOperationException(
                "EventStore data write outcome unexpected. NextExpectedVersion is null");
        }

        return (int)writeResult.NextExpectedVersion;
    }


    private static void CheckConditionalWriteResultStatus(ConditionalWriteResult writeResult, string id)
    {
        switch (writeResult.Status)
        {
            case ConditionalWriteStatus.Succeeded:
                break;
            case ConditionalWriteStatus.VersionMismatch:
                throw new ConcurrencyException(id, null);
            case ConditionalWriteStatus.StreamDeleted:
                throw new InvalidOperationException($"Stream was deleted. StreamId: {id}");
            default:
                throw new InvalidOperationException($"Unexpected write result: {writeResult.Status}");
        }
    }
}
