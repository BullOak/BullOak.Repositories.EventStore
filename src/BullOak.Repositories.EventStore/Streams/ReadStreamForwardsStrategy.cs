namespace BullOak.Repositories.EventStore.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Metadata;

    internal class ReadStreamForwardsStrategy : IStreamReaderStrategy
    {
        private const int SliceSize = 1024; // Max allowed is 4095;

        private readonly string streamId;
        private bool isEndOfStream;

        public ReadStreamForwardsStrategy(string streamId)
        {
            this.streamId = streamId;
        }

        public long NextSliceStart { get; } = StreamPosition.Start;

        public async Task<StreamEventsSlice> GetCurrentSlice(IEventStoreConnection connection, long streamPosition, bool resolveLinkTos)
        {
            var currentSlice = await connection.ReadStreamEventsForwardAsync(streamId, streamPosition, SliceSize, resolveLinkTos);

            isEndOfStream = currentSlice.IsEndOfStream;

            return currentSlice;
        }

        public bool TakeWhile(ItemWithType item) => true;

        public StreamReadResults BuildResults(Dictionary<string, List<(ItemWithType Item, IHoldMetadata Metadata)>> streamsEvents, int currentVersion)
        {
            var readResults = new List<ReadResult>();

            foreach (var stream in streamsEvents)
            {
                var eventStreamId = stream.Key;
                var streamEvents = stream.Value.Select(x => x.Item);

                readResults.AddRange(streamEvents.Select(itemWithType => new ReadResult(eventStreamId, itemWithType)));
            }

            return new StreamReadResults(readResults, currentVersion);
        }

        public bool CanRead => !isEndOfStream;
    }
}
