namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Events;
    using global::EventStore.ClientAPI;
    using Metadata;
    using StateEmit;

    internal class EventReader : IReadEventsFromStream
    {
        private readonly ICreateStateInstances stateFactory;
        private IEventStoreConnection ESConnection => esConnection.Connection;
        private readonly IKeepESConnectionAlive esConnection;

        public EventReader(IKeepESConnectionAlive esConnection, IHoldAllConfiguration configuration)
        {
            this.esConnection = esConnection ?? throw new ArgumentNullException(nameof(esConnection));
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<StreamReadResults> ReadFrom(IStreamReaderStrategy streamReaderStrategy, DateTime? appliesAt = null)
        {
            checked
            {
                var currentVersion = -1;
                var nextSliceStart = streamReaderStrategy.NextSliceStart;
                var streamsEvents = new List<KeyValuePair<string, (ItemWithType Item, IHoldMetadata Metadata)>>();

                do
                {
                    var currentSlice = await streamReaderStrategy.GetCurrentSlice(ESConnection, nextSliceStart, true);

                    if (currentSlice.Status == SliceReadStatus.StreamDeleted || currentSlice.Status == SliceReadStatus.StreamNotFound)
                    {
                        currentVersion = -1;

                        break;
                    }

                    nextSliceStart = currentSlice.NextEventNumber;

                    var newEvents =
                        currentSlice.Events
                            .Select(x => new KeyValuePair<string, (ItemWithType Item, IHoldMetadata Metadata)>(x.Event.EventStreamId, x.ToItemWithType(stateFactory)))
                            .TakeWhile(deserialised => streamReaderStrategy.TakeWhile(deserialised.Value.Item))
                            .Where(deserialised => !appliesAt.HasValue || deserialised.Value.Metadata.ShouldInclude(appliesAt.Value))
                            .ToList();

                    if (currentVersion == -1)
                    {
                        currentVersion = (int)currentSlice.LastEventNumber;
                    }

                    streamsEvents.AddRange(newEvents);

                } while (streamReaderStrategy.CanRead);

                return streamReaderStrategy.BuildResults(GroupEventsByEventStreamId(streamsEvents), currentVersion);
            }
        }

        private static Dictionary<string, List<(ItemWithType Item, IHoldMetadata Metadata)>> GroupEventsByEventStreamId(List<KeyValuePair<string, (ItemWithType Item, IHoldMetadata Metadata)>> streamsEvents)
        {
            var streamsEventDictionary = new Dictionary<string, List<(ItemWithType Item, IHoldMetadata Metadata)>>();

            foreach (var eventWithId in streamsEvents)
            {
                if (streamsEventDictionary.ContainsKey(eventWithId.Key))
                    streamsEventDictionary[eventWithId.Key].Add(eventWithId.Value);
                else
                    streamsEventDictionary.Add(eventWithId.Key,
                        new List<(ItemWithType Item, IHoldMetadata Metadata)> { eventWithId.Value });
            }

            return streamsEventDictionary;
        }
    }
}
