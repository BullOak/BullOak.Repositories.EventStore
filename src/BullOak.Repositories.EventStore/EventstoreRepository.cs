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

    public class EventStoreRepository<TId, TState> : IStartSessions<TId, TState>, IEventStoreStreamDeleter<TId>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IValidateState<TState> stateValidator;

        private readonly IReadEventsFromStream eventReader;
        private readonly IStoreEventsToStream eventWriter;

        private static readonly AlwaysPassValidator<TState> defaultValidator = new();
        private static readonly Func<IAmAStoredEvent, bool> alwaysPassPredicate = _ => true;

        public EventStoreRepository
        (
            IValidateState<TState> stateValidator,
            IHoldAllConfiguration configs,
            IReadEventsFromStream eventReader,
            IStoreEventsToStream eventWriter,
            IDateTimeProvider dateTimeProvider = null
        )
        {
            this.stateValidator = stateValidator ?? throw new ArgumentNullException(nameof(stateValidator));
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));


            this.eventReader = eventReader;
            this.eventWriter = eventWriter;

            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
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

        public async Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists = false,
            DateTime? appliesAt = null)
        {
            if (throwIfNotExists && !(await Contains(id)))
                throw new StreamNotFoundException(id.ToString());

            var session = new EventStoreSession<TState>(stateValidator, configs, eventReader, eventWriter, id.ToString(), dateTimeProvider);
            await session.Initialize(appliesAt.HasValue
                ? e => GetBeforeDateEventPredicate(e, appliesAt.Value)
                : alwaysPassPredicate);

            return session;
        }

        private bool GetBeforeDateEventPredicate(IAmAStoredEvent @event, DateTime appliesAt)
        {
            if(@event.Metadata?.TimeStamp == null)
                return true;

            return @event.Metadata.TimeStamp <= appliesAt;
        }

        public async Task<bool> Contains(TId selector)
        {
            try
            {
                var readResult = await eventReader.ReadFrom(selector.ToString());

                return readResult.StreamExists && await readResult.Events.AnyAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }

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
