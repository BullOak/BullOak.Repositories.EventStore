using System.Text.Json;
using EventStore.Client;
using System.Threading;

namespace BullOak.Repositories.EventStore.Streams
{
    using Events;
    using Metadata;
    using StateEmit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class EventReader : IReadEventsFromStream
    {
        private readonly ICreateStateInstances stateFactory;
        private readonly EventStoreClient client;
        private static readonly IAsyncEnumerable<StoredEvent> emptyReadResult = new StoredEvent[0].ToAsyncEnumerable();

        public EventReader(EventStoreClient client, IHoldAllConfiguration configuration)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, Func<IAmAStoredEvent, bool> predicate = null, Direction direction = Direction.Backwards, CancellationToken cancellationToken = default)
        {
            var readResult = client.ReadStreamAsync(direction,
                streamId,
                direction == Direction.Backwards ? StreamPosition.End : StreamPosition.Start,
                resolveLinkTos: true,
                configureOperationOptions: options => options.TimeoutAfter = TimeSpan.FromSeconds(30),
                cancellationToken: cancellationToken);

            bool streamExists = false;
            try
            {
                var readState = await readResult.ReadState;
                streamExists = readState == ReadState.Ok;
            }
#pragma warning disable 168
            catch (StreamDeletedException ex)
            // This happens when the stream is hard-deleted. We don't want to throw in that case
#pragma warning restore 168
            {
                streamExists = false;
            }

            if (!streamExists)
                return new StreamReadResults(emptyReadResult, false, StreamPosition.FromInt64(-1));

            predicate ??= _ => true;

            var lastIndex = (await client.ReadStreamAsync(Direction.Backwards, streamId,
                revision: StreamPosition.End,
                maxCount: 1,
                resolveLinkTos: false).FirstAsync(cancellationToken)).OriginalEventNumber;

            IAsyncEnumerable<StoredEvent> storedEvents;
            if (direction == Direction.Backwards)
            {
                storedEvents = readResult
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, _) => e.Event.ToStoredEvent(stateFactory))
                    .TakeWhile(e => e.DeserializedEvent is not EntitySoftDeleted)
                    .Where(e => predicate(e));
            }
            else
            {
                storedEvents = readResult
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select((e, c) => e.Event.ToStoredEvent(stateFactory))
                    .Where(e => predicate(e));
            }


            return new StreamReadResults(storedEvents, true, lastIndex);
        }
    }
}
