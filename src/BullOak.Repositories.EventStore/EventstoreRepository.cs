using BullOak.Repositories.EventStore.Events;
using BullOak.Repositories.Repository;
using BullOak.Repositories.Session;
using System;
using System.Linq;
using System.Threading.Tasks;
using BullOak.Repositories.EventStore.Streams;

namespace BullOak.Repositories.EventStore
{
    using Exceptions;
    using OneOf;
    public enum OptimizeFor { ShortStreams, LongStreams };

    public class EventStoreRepository<TId, TState> : IStartOprimizeableSessions<TId, TState>, IEventStoreStreamDeleter<TId>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IValidateState<TState> stateValidator;

        private readonly IReadEventsFromStream eventReader;
        private readonly IStoreEventsToStream eventWriter;

        private static readonly AlwaysPassValidator<TState> defaultValidator = new();

        private readonly string streamNamePrefix;
        public string StreamNamePrefix => streamNamePrefix;

        public EventStoreRepository
        (
            IValidateState<TState> stateValidator,
            IHoldAllConfiguration configs,
            IReadEventsFromStream eventReader,
            IStoreEventsToStream eventWriter,
            IDateTimeProvider dateTimeProvider = null,
            string streamNamePrefix = null
        )
        {
            this.stateValidator = stateValidator ?? throw new ArgumentNullException(nameof(stateValidator));
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));

            this.eventReader = eventReader;
            this.eventWriter = eventWriter;

            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();

            this.streamNamePrefix = streamNamePrefix ?? String.Empty;
        }

        public EventStoreRepository
        (
            IHoldAllConfiguration configs,
            IReadEventsFromStream eventReader,
            IStoreEventsToStream eventWriter
        )
            : this(defaultValidator, configs, eventReader, eventWriter)
        {
        }
        public Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists,
            DateTime? appliesAt)
            => BeginSessionFor(id, throwIfNotExists, appliesAt, OptimizeFor.ShortStreams);

        public async Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists = false,
            DateTime? appliesAt = null, OptimizeFor optimization = OptimizeFor.ShortStreams)
        {
            var streamName = id.ToString();
            Func<StoredEvent, bool> readPredicate = appliesAt.HasValue
                    ? e => GetBeforeDateEventPredicate(e, appliesAt.Value)
                    : AlwaysPassPredicate;

            var readResults = await eventReader.ReadFrom(streamName, readPredicate);
            var streamExists = readResults.StreamExists;

            if (throwIfNotExists && !streamExists)
                throw new StreamNotFoundException(streamName);

            var session = new EventStoreSession<TState>(stateValidator, configs, streamExists, eventWriter, streamName, dateTimeProvider);

            switch(optimization)
            {
                case OptimizeFor.ShortStreams:
                    {
                        session.LoadFromEvents(await readResults.Events.Select(ToBOStoredEvent).ToArrayAsync());

                        break;
                    }
                case OptimizeFor.LongStreams:
                    {
                        await session.LoadFromEvents(readResults.Events.Select(ToBOStoredEvent));

                        break;
                    }
            }

            return session;
        }

        private static Appliers.StoredEvent ToBOStoredEvent(StoredEvent se)
            => new Appliers.StoredEvent(se.EventType, se.DeserializedEvent, se.PositionInStream);

        private bool GetBeforeDateEventPredicate(StoredEvent @event, DateTime appliesAt)
        {
            if(@event.Metadata?.TimeStamp == null)
                return true;

            return @event.Metadata.TimeStamp <= appliesAt;
        }

        public async Task<bool> Contains(TId selector)
        {
            var readResult = await eventReader.ReadFrom(selector.ToString());

            return readResult.StreamExists; // The reader returns that the stream doesn't exist if it's entirely empty.
        }

        private static bool AlwaysPassPredicate(StoredEvent e) => true;

        /// <summary>
        /// This is provided to implement the <see cref="IStartSessions{TEntitySelector,TState}"/> interface,
        /// but is not ideally how event store streams should be deleted.
        /// Please use the <see cref="IEventStoreStreamDeleter{TId}"/> interface this class also implements.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// This Delete implementation corresponds to an EventStore soft-delete, which is actually what BullOak
        /// would consider to be a hard-delete; the scavenger will eventually delete even a soft-deleted stream so if
        /// you want proper soft-delete semantics then use <see cref="SoftDeleteByEvent"/> or
        /// <see cref="SoftDeleteByEvent{TSoftDeleteEventType}"/>
        [Obsolete(
            "Please use either IEventStoreStreamDeleter.SoftDelete or IEventStoreStreamDeleter.SoftDeleteByEvent")]
        public async Task Delete(TId selector)
        {
            await SoftDelete(selector);
        }

        //[Obsolete("This doesn't care for other operations. In the future it will be moved to session or renamed.")]
        public async Task SoftDelete(TId selector)
        {
            var readResult = await eventReader.ReadFrom(selector.ToString());

            if (readResult.StreamExists)
                await eventWriter.SoftDelete(selector.ToString());
        }
    }
}
