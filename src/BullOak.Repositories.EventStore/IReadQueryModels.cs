namespace BullOak.Repositories.EventStore
{
    using System.Threading.Tasks;

    public interface IReadQueryModels<TId, TState>
    {
        Task<ReadModel<TState>> ReadFrom(TId id);
    }
}