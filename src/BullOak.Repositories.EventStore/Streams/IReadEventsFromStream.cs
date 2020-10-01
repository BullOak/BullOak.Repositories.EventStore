namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Threading.Tasks;

    internal interface IReadEventsFromStream
    {
        Task<StreamReadResults> ReadFrom(IStreamReaderStrategy streamReaderStrategy, DateTime? appliesAt = null);
    }
}
