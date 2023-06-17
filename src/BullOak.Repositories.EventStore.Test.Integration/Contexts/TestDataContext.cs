namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;
    using Streams;
    using System.Threading.Tasks;
    using System.Linq;
    using BullOak.Repositories.EventStore.Events;

    public struct IntegrationStreamReadResults
    {
        public readonly IEnumerable<StoredEvent> Events;
        public readonly bool StreamExists;

        private IntegrationStreamReadResults(IEnumerable<StoredEvent> events, bool streamExists)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            StreamExists = streamExists;
        }

        public async static Task<IntegrationStreamReadResults> FromStreamReadResult(StreamReadResults readResults)
        {
            var events = await readResults.Events.ToListAsync();

            return new IntegrationStreamReadResults(events, readResults.StreamExists);
        }
    }

    internal class TestDataContext
    {
        internal string StreamIdPrefix = "Stream_Prefix";

        internal string CurrentStreamId { get; set; }

        internal Guid RawStreamId { get; set; }

        public Exception RecordedException { get; internal set; }
        public IHoldHigherOrder LatestLoadedState { get; internal set; }
        public bool IsNewState { get; internal set; }

        public StreamReadToMemoryResults LoadedStreamResults { get; private set; }
        public Dictionary<string, IManageSessionOf<IHoldHigherOrder>> NamedSessions { get; } = new();
        public Dictionary<string, List<Exception>> NamedSessionsExceptions { get; } = new();

        public long LastConcurrencyId { get; set; }

        internal List<IMyEvent> LastGeneratedEvents = new List<IMyEvent>();

        internal async Task SetReadResults(StreamReadResults readResults)
            => LoadedStreamResults = new StreamReadToMemoryResults(await readResults.Events.ToArrayAsync(), readResults.StreamExists);
        internal Task SetReadResults(StreamReadToMemoryResults readResults)
        { LoadedStreamResults = readResults; return Task.CompletedTask; }

        internal void ResetStream(string categoryName = null)
        {
            RawStreamId = Guid.NewGuid();
            StreamIdPrefix = !string.IsNullOrEmpty(categoryName) ? $"{StreamIdPrefix}_{categoryName}" : StreamIdPrefix;
            CurrentStreamId =  $"{StreamIdPrefix}-{RawStreamId}";
        }
    }
}
