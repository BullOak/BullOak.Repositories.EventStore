namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;
    using Events;

    public struct StreamReadResults
    {
        public readonly IAsyncEnumerable<StoredEvent> Events;
        public readonly bool StreamExists;

        public StreamReadResults(IAsyncEnumerable<StoredEvent> events, bool streamExists)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            StreamExists = streamExists;
        }
    }
}
