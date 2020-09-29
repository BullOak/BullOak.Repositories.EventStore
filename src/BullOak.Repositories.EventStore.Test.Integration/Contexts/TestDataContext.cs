namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Components;
    using Session;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class TestDataContext
    {
        private static readonly Random random = new Random();

        private static readonly string UniqueCategoryName = RandomString(15);

        internal readonly string StreamIdPrefix = $"Stream_Prefix_{UniqueCategoryName}";

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

        internal void ResetStream()
        {
            RawStreamId = Guid.NewGuid();
            CurrentStreamId = $"{StreamIdPrefix}-{RawStreamId}";
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
