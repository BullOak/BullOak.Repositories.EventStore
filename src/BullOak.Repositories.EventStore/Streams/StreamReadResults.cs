using BullOak.Repositories.EventStore.Events;
using EventStore.Client;

namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;

    internal struct StreamReadResults
    {
        public readonly IAsyncEnumerable<StoredEvent> events;
        public readonly bool streamExists;
        public readonly StreamPosition streamPosition;

        public StreamReadResults(IAsyncEnumerable<StoredEvent> events, bool streamExists, StreamPosition streamPosition)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.streamExists = streamExists;
            this.streamPosition = streamPosition;
        }
    }
}
