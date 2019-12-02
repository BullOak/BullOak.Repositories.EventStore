namespace BullOak.Repositories.EventStore.Test.Integration.Contexts.EventStoreIsolation
{
    using System;

    internal class NoopIsolation : IDisposable
    {
        // NO-OP
        // Expects EventStore server is running on localhost:1113

        public void Dispose()
        {}
    }
}
