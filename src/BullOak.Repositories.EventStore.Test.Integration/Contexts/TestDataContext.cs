namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;

    internal class TestDataContext
    {
        internal readonly string StreamIdPrefix = "Stream_Prefix";

        internal string CurrentStreamId { get; set; }

        internal Guid RawStreamId { get; set; }

        public Exception RecordedException { get; internal set; }
        public IHoldHigherOrder LatestLoadedState { get; internal set; }
        public Dictionary<string, IManageSessionOf<IHoldHigherOrder>> NamedSessions { get; internal set; } =
            new Dictionary<string, IManageSessionOf<IHoldHigherOrder>>();
        public Dictionary<string, List<Exception>> NamedSessionsExceptions { get; internal set; } =
            new Dictionary<string, List<Exception>>();

        public int LastConcurrencyId { get; set; }

        internal List<MyEvent> LastGeneratedEvents = new List<MyEvent>();

        internal void ResetStream(string categoryName = null)
        {
            RawStreamId = Guid.NewGuid();
            CurrentStreamId = !string.IsNullOrEmpty(categoryName) ? $"{StreamIdPrefix}_{categoryName}-{RawStreamId}" : $"{StreamIdPrefix}-{RawStreamId}";
        }
    }
}
