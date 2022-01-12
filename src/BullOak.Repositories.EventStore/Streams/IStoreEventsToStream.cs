namespace BullOak.Repositories.EventStore.Streams;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IStoreEventsToStream
{
    Task<int> Add(
        string streamId,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken = default);

    Task<int> AppendTo(
        string streamId,
        long revision,
        ItemWithType[] eventsToAdd,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken = default);

    Task SoftDelete(string streamId);
    Task SoftDeleteByEvent<T>(string streamId, IEnumerable<T> eventData);
}
