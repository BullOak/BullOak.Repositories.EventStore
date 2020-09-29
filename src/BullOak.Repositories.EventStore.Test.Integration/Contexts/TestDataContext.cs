namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;

    internal class TestDataContext
    {
        internal List<Guid> StreamIds { get; } = new List<Guid>();
        internal Guid CurrentStreamId { get; set; }
        public Exception RecordedException { get; internal set; }
        public IHoldHigherOrder LatestLoadedState { get; internal set; }
        public Dictionary<string, IManageSessionOf<IHoldHigherOrder>> NamedSessions { get; internal set; } =
            new Dictionary<string, IManageSessionOf<IHoldHigherOrder>>();
        public Dictionary<string, List<Exception>> NamedSessionsExceptions { get; internal set; } =
            new Dictionary<string, List<Exception>>();

        public int LastConcurrencyId { get; set; }

        internal List<MyEvent> LastGeneratedEvents;

        internal void ResetStream()
        {
            StreamIds.Clear();
            CurrentStreamId = Guid.NewGuid();
            StreamIds.Add(CurrentStreamId);
        }

        internal void AddNewStream()
        {
            StreamIds.Add(Guid.NewGuid());
        }
    }
}
