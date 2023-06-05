using System.Collections.Generic;
using System.Threading;
using BullOak.Repositories.EventStore.Events;

namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Threading.Tasks;

    public interface IReadEventsFromStream
    {
        Task<StreamReadResults> ReadFrom(string streamId,
            Func<IAmAStoredEvent, bool> predicate = null,
            StreamReadDirection direction = StreamReadDirection.Forwards,
            CancellationToken cancellationToken = default);
    }
}
