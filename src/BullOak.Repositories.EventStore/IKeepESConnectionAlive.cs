using EventStore.ClientAPI;
using System;

namespace BullOak.Repositories.EventStore
{
    public interface IKeepESConnectionAlive : IDisposable
    {
        IEventStoreConnection Connection { get; }
    }
}
