using EventStore.ClientAPI;

namespace BullOak.Repositories.EventStore
{
    internal class EventStoreConnectionContainer : IKeepESConnectionAlive
    {
        public IEventStoreConnection Connection { get; private set; }

        public EventStoreConnectionContainer(IEventStoreConnection connection)
            => Connection = connection;

        public void Dispose() { }
    }
}
