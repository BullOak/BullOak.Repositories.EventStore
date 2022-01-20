namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;
    using Events;

    public struct StreamReadResults
    {
        public readonly IAsyncEnumerable<StoredEvent> Events;
        public readonly bool StreamExists;
        public readonly StoredEventPosition StoredEventPosition;

        public StreamReadResults(IAsyncEnumerable<StoredEvent> events, bool streamExists, StoredEventPosition storedEventPosition)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            StreamExists = streamExists;
            StoredEventPosition = storedEventPosition;
        }
    }
}
