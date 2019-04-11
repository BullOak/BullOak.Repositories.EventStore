namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BullOak.Repositories.EventStore.Events;
    using BullOak.Repositories.StateEmit;
    using global::EventStore.ClientAPI;

    internal class EventReader : IReadEventsFromStream
    {
        private const int SliceSize = 1024; //4095 is max allowed value

        private static readonly Type SoftDeleteEventType = typeof(SoftDeleteEvent);

        private readonly ICreateStateInstances stateFactory;
        private readonly IEventStoreConnection eventStoreConnection;

        public EventReader(IEventStoreConnection connection, IHoldAllConfiguration configuration)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            eventStoreConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<StreamReadResults> ReadFrom(string streamId)
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
                        currentSlice.Events.Select(x => x.ToItemWithType(stateFactory))
                            .TakeWhile(itemWithType =>
                            {
                                foundSoftDelete = itemWithType.type == SoftDeleteEventType;
                                return !foundSoftDelete;
                            });
                    events.AddRange(newEvents);

                    currentVersion = Math.Max(currentVersion, (int) currentSlice.LastEventNumber);
                } while (nextSliceStart != -1 && !foundSoftDelete);

                events.Reverse();
                return new StreamReadResults(events, currentVersion);
            }
        }
    }
}
