namespace BullOak.Repositories.EventStore.Streams
{
    using Events;
    using StateEmit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using System.Threading;
    using static global::EventStore.Client.EventStoreClient;

    public class GrpcEventReader : IReadEventsFromStream
    {
        private readonly ICreateStateInstances stateFactory;
        private readonly EventStoreClient client;
        private static readonly IAsyncEnumerable<StoredEvent> EmptyReadResult = Array.Empty<StoredEvent>().ToAsyncEnumerable();

        public GrpcEventReader(EventStoreClient client, IHoldAllConfiguration configuration)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, Func<IAmAStoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Forwards, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                direction = StreamReadDirection.Forwards;

            var readResult = client.ReadStreamAsync(
                direction == StreamReadDirection.Backwards ? Direction.Backwards : Direction.Forwards,
                streamId,
                direction == StreamReadDirection.Backwards ? StreamPosition.End : StreamPosition.Start,
                resolveLinkTos: true,
                deadline: TimeSpan.FromSeconds(30),
                cancellationToken: cancellationToken);

            if (!await StreamExists(readResult))
                return new StreamReadResults(EmptyReadResult, true, StoredEventPosition.FromInt64(-1));

            predicate ??= _ => true;

            var lastEvent = (await client.ReadStreamAsync(Direction.Backwards, streamId,
                StreamPosition.End,
                1,
                deadline: TimeSpan.FromSeconds(30),
                resolveLinkTos: false).FirstAsync(cancellationToken));
            var lastIndex = lastEvent.OriginalEventNumber;

            if(lastEvent.Event.ToStoredEvent(stateFactory).DeserializedEvent is EntitySoftDeleted)
                return new StreamReadResults(EmptyReadResult, true, StoredEventPosition.FromInt64(-1));

            IAsyncEnumerable<StoredEvent> storedEvents;
            if (direction == StreamReadDirection.Backwards)
            {
                storedEvents = readResult
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select(e => e.Event.ToStoredEvent(stateFactory))
                    .TakeWhile(e => e.DeserializedEvent is not EntitySoftDeleted)
                    .Where(e => predicate(e));
            }
            else
            {
                storedEvents = readResult
                    // Trust me, resharper is wrong in this one. Event can be null
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(e => e.Event != null)
                    .Select(e => e.Event.ToStoredEvent(stateFactory))
                    .Where(e => predicate(e));
            }

            return new StreamReadResults(storedEvents, true, StoredEventPosition.FromInt64(lastIndex.ToInt64()));
        }

        private async Task<bool> StreamExists(ReadStreamResult readResult)
        {
            try
            {
                var readState = await readResult.ReadState;
                return readState == ReadState.Ok;
            }
#pragma warning disable 168
            catch (StreamDeletedException ex)
            // This happens when the stream is hard-deleted. We don't want to throw in that case
#pragma warning restore 168
            {
                return false;
            }
        }
    }
}
