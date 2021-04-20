namespace BullOak.Repositories.EventStore
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IReadEvents<TId>
    {
        Task<IEnumerable<ItemWithType>> ReadFrom(TId id);
    }
}
