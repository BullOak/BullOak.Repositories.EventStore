namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Threading.Tasks;

    public interface IReadQueryModels<in TId, TState>
    {
        Task<ReadModel<TState>> ReadFrom(TId id);
        Task<TState> ReadFrom(TId id, DateTime appliesAt);
    }
}
