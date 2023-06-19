namespace BullOak.Repositories.EventStore.Streams
{
    using Events;
    using StateEmit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using System.Threading;
    using global::EventStore.ClientAPI.Exceptions;

    public class TcpEventReader : IReadEventsFromStream
    {
        private readonly ICreateStateInstances stateFactory;
        private readonly IEventStoreConnection connection;
        private readonly int pageSize;
        private static readonly IAsyncEnumerable<StoredEvent> EmptyReadResult = Array.Empty<StoredEvent>().ToAsyncEnumerable();
        private static readonly StoredEvent[] EmptyStoredEvents = new StoredEvent[0];

        public TcpEventReader(IEventStoreConnection client, IHoldAllConfiguration configuration, int pageSize = 4096)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            this.connection = client ?? throw new ArgumentNullException(nameof(client));
            this.pageSize = pageSize;
        }

        public async Task<StreamReadToMemoryResults> ReadToMemoryFrom(string streamId, Func<StoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Forwards, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                direction = StreamReadDirection.Forwards;

            predicate ??= _ => true;

            StoredEvent[] storedEvents;
            if (direction == StreamReadDirection.Backwards)
            {
                var readResult = await connection.ReadStreamEventsBackwardAsync(streamId, StreamPosition.End, pageSize, true);

                if (!StreamExists(readResult))
                    return new StreamReadToMemoryResults(EmptyStoredEvents, false);

                storedEvents = readResult.Events
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, _) => e.Event.ToStoredEvent(stateFactory))
                    .TakeWhile(e => e.DeserializedEvent is not EntitySoftDeleted)
                    .Where(e => predicate(e))
                    .ToArray();
            }
            else
            {
                var readResult = await connection.ReadStreamEventsForwardAsync(streamId, StreamPosition.Start, pageSize, true);

                if (!StreamExists(readResult))
                    return new StreamReadToMemoryResults(EmptyStoredEvents, false);

                storedEvents = readResult.Events
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, c) => e.Event.ToStoredEvent(stateFactory))
                    .Where(e => predicate(e))
                    .ToArray();
            }

            return new StreamReadToMemoryResults(storedEvents, true);
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, Func<StoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Forwards, CancellationToken cancellationToken = default)
        {
            var readToMemResults = await ReadToMemoryFrom(streamId, predicate, direction, cancellationToken);
            return new StreamReadResults(readToMemResults.Events.ToAsyncEnumerable(), readToMemResults.StreamExists);
        }

        private static bool StreamExists(StreamEventsSlice readResult)
        {
            bool streamExists = false;
            try
            {
                var readState = readResult.Status;
                streamExists = readState == SliceReadStatus.Success;
            }
#pragma warning disable 168
            catch (StreamDeletedException ex)
                // This happens when the stream is hard-deleted. We don't want to throw in that case
#pragma warning restore 168
            {
                streamExists = false;
            }

            return streamExists;
        }
    }
}
