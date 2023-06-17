namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

    public struct StreamReadToMemoryResults
    {
        public readonly StoredEvent[] Events;
        public readonly bool StreamExists;

        internal static async Task<StreamReadToMemoryResults> FromStreamReadResults(StreamReadResults readResults)
        {
            var events = await readResults.Events.ToArrayAsync();
            return new StreamReadToMemoryResults(events, readResults.StreamExists);
        }

        public StreamReadToMemoryResults(StoredEvent[] events, bool streamExists)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            StreamExists = streamExists;
        }
    }
}
