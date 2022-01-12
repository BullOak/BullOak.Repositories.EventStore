using System.Collections.Generic;
using System.Threading;
using BullOak.Repositories.EventStore.Events;
using EventStore.Client;

namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Threading.Tasks;

    public interface IReadEventsFromStream
    {
        Task<StreamReadResults> ReadFrom(string streamId,
            Func<IAmAStoredEvent, bool> predicate = null,
            Direction direction = Direction.Backwards,
            CancellationToken cancellationToken = default);
    }
}
