﻿namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Events;
    using global::EventStore.ClientAPI;
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

        public async Task<StreamReadResults> ReadFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            checked
            {
                int currentVersion = -1;
                var events = new List<ItemWithType>();
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
                            .Select(x => x.ToItemWithType(stateFactory))
                            .Where(deserialised => !appliesAt.HasValue || deserialised.Metadata.ShouldInclude(appliesAt.Value))
                            .Select(x => x.Item);

                    events.AddRange(newEvents);

                    endOfStream = currentSlice.IsEndOfStream;

                    if(endOfStream)
                        currentVersion = (int)currentSlice.LastEventNumber;

                } while (!endOfStream);

                return new StreamReadResults(events, currentVersion);
            }
        }
    }
}
