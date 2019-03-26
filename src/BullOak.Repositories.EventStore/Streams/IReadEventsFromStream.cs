namespace BullOak.Repositories.EventStore
{
    using System.Threading.Tasks;

    internal interface IReadEventsFromStream
    {
        Task<StreamReadResults> ReadFrom(string streamId);
    }
}