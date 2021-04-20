﻿namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;

    internal class TestDataContext
    {
        internal string StreamIdPrefix = "Stream_Prefix";

        internal string CurrentStreamId { get; set; }

        internal int CurrentVersion { get; set; } = -1;

        internal Guid RawStreamId { get; set; }

        public Exception RecordedException { get; internal set; }
        public IHoldHigherOrder LatestLoadedState { get; internal set; }
        public IEnumerable<ItemWithType> LatestReadEvents { get; internal set; }

        public Dictionary<string, IManageSessionOf<IHoldHigherOrder>> NamedSessions { get; internal set; } =
            new Dictionary<string, IManageSessionOf<IHoldHigherOrder>>();
        public Dictionary<string, List<Exception>> NamedSessionsExceptions { get; internal set; } =
            new Dictionary<string, List<Exception>>();

        public int LastConcurrencyId { get; set; }

        internal List<IMyEvent> LastGeneratedEvents = new List<IMyEvent>();

        internal void ResetStream(string categoryName = null)
        {
            RawStreamId = Guid.NewGuid();
            StreamIdPrefix = !string.IsNullOrEmpty(categoryName) ? $"{StreamIdPrefix}_{categoryName}" : StreamIdPrefix;
            CurrentStreamId =  $"{StreamIdPrefix}-{RawStreamId}";
            CurrentVersion = -1;
        }
    }
}
