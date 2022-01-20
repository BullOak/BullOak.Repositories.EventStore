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

        public TcpEventReader(IEventStoreConnection client, IHoldAllConfiguration configuration, int pageSize = 4096)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            this.connection = client ?? throw new ArgumentNullException(nameof(client));
            this.pageSize = pageSize;
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, Func<IAmAStoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Backwards, CancellationToken cancellationToken = default)
        {
            predicate ??= _ => true;

            IAsyncEnumerable<StoredEvent> storedEvents;
            if (direction == StreamReadDirection.Backwards)
            {
                var readResult = await connection.ReadStreamEventsBackwardAsync(streamId, StreamPosition.End, pageSize, true);

                if (!StreamExists(readResult))
                    return new StreamReadResults(EmptyReadResult, false, StoredEventPosition.FromInt64(-1));

                storedEvents = readResult.Events
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, _) => e.Event.ToStoredEvent(stateFactory))
                    .TakeWhile(e => e.DeserializedEvent is not EntitySoftDeleted)
                    .Where(e => predicate(e))
                    .ToAsyncEnumerable();
            }
            else
            {
                var readResult = await connection.ReadStreamEventsForwardAsync(streamId, StreamPosition.Start, pageSize, true);

                if (!StreamExists(readResult))
                    return new StreamReadResults(EmptyReadResult, false, StoredEventPosition.FromInt64(-1));

                storedEvents = readResult.Events
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, c) => e.Event.ToStoredEvent(stateFactory))
                    .Where(e => predicate(e))
                    .ToAsyncEnumerable();
            }

            var result = await connection.ReadEventAsync(streamId, StreamPosition.End, false);
            var idx = result.Event?.OriginalEventNumber ?? -1;
            return new StreamReadResults(storedEvents, true, StoredEventPosition.FromInt64(idx));
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
