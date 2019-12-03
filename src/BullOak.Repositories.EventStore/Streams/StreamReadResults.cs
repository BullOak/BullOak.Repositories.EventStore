namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;

    internal struct StreamReadResults
    {
        public readonly IEnumerable<ItemWithType> events;
        public readonly int streamVersion;

        public StreamReadResults(IEnumerable<ItemWithType> events, int streamVersion)
        {
            this.streamVersion = streamVersion;
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }
    }
}
