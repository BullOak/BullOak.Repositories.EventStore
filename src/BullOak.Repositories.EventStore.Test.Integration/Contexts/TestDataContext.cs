namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;
    using Streams;

    internal class TestDataContext
    {
        internal string StreamIdPrefix = "Stream_Prefix";

        internal string CurrentStreamId { get; set; }

        internal Guid RawStreamId { get; set; }

        public Exception RecordedException { get; internal set; }
        public IHoldHigherOrder LatestLoadedState { get; internal set; }

        public StreamReadResults LatestStreamReadResults { get; internal set; }
        public Dictionary<string, IManageSessionOf<IHoldHigherOrder>> NamedSessions { get; } = new();
        public Dictionary<string, List<Exception>> NamedSessionsExceptions { get; } = new();

        public int LastConcurrencyId { get; set; }

        internal List<IMyEvent> LastGeneratedEvents = new List<IMyEvent>();

        internal void ResetStream(string categoryName = null)
        {
            RawStreamId = Guid.NewGuid();
            StreamIdPrefix = !string.IsNullOrEmpty(categoryName) ? $"{StreamIdPrefix}_{categoryName}" : StreamIdPrefix;
            CurrentStreamId =  $"{StreamIdPrefix}-{RawStreamId}";
        }
    }
}
