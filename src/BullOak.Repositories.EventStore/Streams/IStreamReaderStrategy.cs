namespace BullOak.Repositories.EventStore.Streams
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Metadata;

    internal interface IStreamReaderStrategy
    {
        long NextSliceStart { get; }
        Task<StreamEventsSlice> GetCurrentSlice(IEventStoreConnection connection, long streamPosition, bool resolveLinkTos);
        bool TakeWhile(ItemWithType item);
        StreamReadResults BuildResults(Dictionary<string, List<(ItemWithType Item, IHoldMetadata Metadata)>> streamsEvents, int currentVersion);
        bool CanRead { get; }
    }
}
