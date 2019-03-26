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
                int currentVersion;
                var events = new List<ItemWithType>();
                StreamEventsSlice currentSlice;
                long nextSliceStart = StreamPosition.Start;
                do
                {
                    currentSlice =
                        await eventStoreConnection.ReadStreamEventsForwardAsync(streamId, nextSliceStart, SliceSize,
                            true);
                    if (currentSlice.Status == SliceReadStatus.StreamDeleted ||
                        currentSlice.Status == SliceReadStatus.StreamNotFound)
                    {
                        currentVersion = -1;

                        break;
                    }

                    nextSliceStart = currentSlice.NextEventNumber;
                    events.AddRange(currentSlice.Events.Select(x=> x.ToItemWithType(stateFactory)));
                    currentVersion = (int) currentSlice.LastEventNumber;
                } while (!currentSlice.IsEndOfStream);

                return new StreamReadResults(events, currentVersion);
            }
        }
    }
}
