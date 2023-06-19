using BullOak.Repositories.Repository;
using BullOak.Repositories.Session;
using System;
using System.Threading.Tasks;

namespace BullOak.Repositories.EventStore
{
    public interface IStartOprimizeableSessions<TId, TState> : IStartSessions<TId, TState>
    {
        Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists = false,
            DateTime? appliesAt = null, OptimizeFor optimization = OptimizeFor.ShortStreams);
    }
}
