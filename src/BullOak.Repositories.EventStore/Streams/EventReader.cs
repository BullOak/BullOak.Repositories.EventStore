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
        private const int SliceSize = 1024; //4095 is max allowed value

        private readonly ICreateStateInstances stateFactory;
        private readonly IEventStoreConnection eventStoreConnection;

        public EventReader(IEventStoreConnection connection, IHoldAllConfiguration configuration)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            eventStoreConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, DateTime? appliesAt = null)
        {
            checked
            {
                int currentVersion = -1;
                bool foundSoftDelete = false;
                var events = new List<ItemWithType>();
                StreamEventsSlice currentSlice;
                long nextSliceStart = StreamPosition.End;
                do
                {
                    currentSlice =
                        await eventStoreConnection.ReadStreamEventsBackwardAsync(streamId, nextSliceStart,
                            SliceSize, true);
                    if (currentSlice.Status == SliceReadStatus.StreamDeleted ||
                        currentSlice.Status == SliceReadStatus.StreamNotFound)
                    {
                        currentVersion = -1;

                        break;
                    }

                    nextSliceStart = currentSlice.NextEventNumber;
                    var newEvents =
                        currentSlice.Events
                            .Select(x => x.ToItemWithType(stateFactory))
                            .TakeWhile(deserialised =>
                            {
                                foundSoftDelete = deserialised.Item.IsSoftDeleteEvent();
                                return !foundSoftDelete;
                            })
                            .Where(deserialised => !appliesAt.HasValue || deserialised.Metadata.ShouldInclude(appliesAt.Value))
                            .Select(x => x.Item);

                    events.AddRange(newEvents);

                    if (currentVersion == -1)
                    {
                        currentVersion = (int)currentSlice.LastEventNumber;
                    }
                } while (nextSliceStart != -1 && !foundSoftDelete);

                events.Reverse();
                return new StreamReadResults(events, currentVersion);
            }
        }

        public async Task<IEnumerable<StreamCategoryReadResults>> ReadFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            checked
            {
                int currentVersion = -1;
                var events = new Dictionary<string, List<ItemWithType>>();
                bool endOfStream;
                long nextSliceStart = StreamPosition.Start;
                do
                {
                    var currentSlice = await eventStoreConnection.ReadStreamEventsForwardAsync(categoryName, nextSliceStart,
                    SliceSize, true);

                    if (currentSlice.Status == SliceReadStatus.StreamDeleted || currentSlice.Status == SliceReadStatus.StreamNotFound)
                    {
                        break;
                    }

                    nextSliceStart = currentSlice.NextEventNumber;
                    var newEvents =
                        currentSlice.Events
                            .Select(x => new KeyValuePair<string, (ItemWithType Item, IHoldMetadata Metadata)>(x.Event.EventStreamId, x.ToItemWithType(stateFactory)))
                            .Where(deserialised => !appliesAt.HasValue || deserialised.Value.Metadata.ShouldInclude(appliesAt.Value))
                            .Select(x => new KeyValuePair<string, ItemWithType>(x.Key, x.Value.Item));

                    foreach (var eventWithId in newEvents)
                    {
                        if (events.ContainsKey(eventWithId.Key))
                            events[eventWithId.Key].Add(eventWithId.Value);
                        else
                            events.Add(eventWithId.Key, new List<ItemWithType> { eventWithId.Value });
                    }

                    endOfStream = currentSlice.IsEndOfStream;

                    if (endOfStream)
                        currentVersion = (int)currentSlice.LastEventNumber;

                } while (!endOfStream);

                return events.Select(@event => new StreamCategoryReadResults(@event.Value, @event.Key, currentVersion)).ToList();
            }
        }
    }
}
