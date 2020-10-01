namespace BullOak.Repositories.EventStore.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Metadata;

    internal class ReadStreamBackwardsStrategy : IStreamReaderStrategy
    {
        private const int SliceSize = 1024; // Max allowed is 4095;
        private readonly string streamId;
        private long nextSliceStart;
        private bool foundSoftDelete;

        public ReadStreamBackwardsStrategy(string streamId)
        {
            this.streamId = streamId;
        }

        public long NextSliceStart { get; } = StreamPosition.End;

        public async Task<StreamEventsSlice> GetCurrentSlice(IEventStoreConnection connection, long streamPosition, bool resolveLinkTos)
        {
            var currentSlice = await connection.ReadStreamEventsBackwardAsync(streamId, streamPosition, SliceSize, resolveLinkTos);

            nextSliceStart = currentSlice.NextEventNumber;

            return currentSlice;
        }

        public bool TakeWhile(ItemWithType item)
        {
            foundSoftDelete = item.IsSoftDeleteEvent();

            return !foundSoftDelete;
        }

        public StreamReadResults BuildResults(Dictionary<string, List<(ItemWithType Item, IHoldMetadata Metadata)>> streamsEvents, int currentVersion)
        {
            var readResults = new List<ReadResult>();

            foreach (var eventStream in streamsEvents)
            {
                eventStream.Value.Reverse();

                var eventStreamId = eventStream.Key;
                var streamEvents = eventStream.Value.Select(x => x.Item);

                readResults.AddRange(streamEvents.Select(itemWithType => new ReadResult(eventStreamId, itemWithType)));
            }

            return new StreamReadResults(readResults, currentVersion);
        }

        public bool CanRead => nextSliceStart != -1 && !foundSoftDelete;
    }
}
