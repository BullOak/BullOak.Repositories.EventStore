namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;

    internal struct StreamReadResults
    {
        public readonly IEnumerable<ReadResult> results;
        public readonly int streamVersion;

        public StreamReadResults(IEnumerable<ReadResult> events, int streamVersion)
        {
            this.streamVersion = streamVersion;
            results = events ?? throw new ArgumentNullException(nameof(events));
        }
    }
}
