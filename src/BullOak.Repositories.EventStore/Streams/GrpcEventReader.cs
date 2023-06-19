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
        private readonly TimeSpan deadline;

        public GrpcEventReader(EventStoreClient client, IHoldAllConfiguration configuration, TimeSpan? deadline = null)
        {
            stateFactory = configuration?.StateFactory ?? throw new ArgumentNullException(nameof(configuration));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.deadline = deadline ?? TimeSpan.FromSeconds(30);
        }

        public async Task<StreamReadResults> ReadFrom(string streamId, Func<StoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Forwards, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                direction = StreamReadDirection.Forwards;

            var readResult = client.ReadStreamAsync(
                direction == StreamReadDirection.Backwards ? Direction.Backwards : Direction.Forwards,
                streamId,
                direction == StreamReadDirection.Backwards ? StreamPosition.End : StreamPosition.Start,
                resolveLinkTos: true,
                deadline: deadline,
                cancellationToken: cancellationToken);
            
            if (!await StreamExists(readResult))
                return new StreamReadResults(EmptyReadResult, false);

            predicate ??= AlwaysPassPredicate;

            IAsyncEnumerable<StoredEvent> storedEvents;
            if (direction == StreamReadDirection.Backwards)
                storedEvents = ProcessReadStream(readResult, predicate, x => x.DeserializedEvent is not EntitySoftDeleted, stateFactory);
            else
                storedEvents = ProcessReadStream(readResult, predicate, AlwaysFailPredicate, stateFactory);

            //TODO: Change int conversions. This sucks
            return new StreamReadResults(storedEvents, true);
        }

        private static bool AlwaysPassPredicate(StoredEvent se) => true;
        private static bool AlwaysFailPredicate(StoredEvent se) => false;

        private static async IAsyncEnumerable<StoredEvent> ProcessReadStream(ReadStreamResult readResult, Func<StoredEvent, bool> filter, Func<StoredEvent, bool> stopPredicate, ICreateStateInstances stateFactory)
        {
            await foreach (var e in readResult)
            {
                if (e.Event != null)
                {
                    var @event = e.Event.ToStoredEvent(stateFactory);
                    if (stopPredicate(@event))
                        break;
                    if (filter(@event))
                        yield return @event;
                }
            }
        }

        public async Task<StreamReadToMemoryResults> ReadToMemoryFrom(string streamId, Func<StoredEvent, bool> predicate = null, StreamReadDirection direction = StreamReadDirection.Forwards, CancellationToken cancellationToken = default)
        {
            var results = await ReadFrom(streamId, predicate, direction, cancellationToken);

            return await StreamReadToMemoryResults.FromStreamReadResults(results);
        }

        private StoredEventPosition ToStoredPosition(StreamPosition? streamPosition)
            => streamPosition.HasValue ? StoredEventPosition.FromInt64(streamPosition.Value.ToInt64()) : StoredEventPosition.FromInt64(-1);

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
