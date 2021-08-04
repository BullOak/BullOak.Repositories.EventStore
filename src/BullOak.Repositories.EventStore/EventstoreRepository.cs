using BullOak.Repositories.EventStore.Events;
using BullOak.Repositories.Repository;
using BullOak.Repositories.Session;
using System;
using System.Linq;
using System.Threading.Tasks;
using BullOak.Repositories.EventStore.Streams;
using EventStore.Client;
using StreamNotFoundException = BullOak.Repositories.Exceptions.StreamNotFoundException;

namespace BullOak.Repositories.EventStore
{
    public class EventStoreRepository<TId, TState> : IStartSessions<TId, TState>, IEventStoreStreamDeleter<TId>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly EventStoreClient client;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IValidateState<TState> stateValidator;
        private readonly EventReader eventReader;

        private static AlwaysPassValidator<TState> defaultValidator = new AlwaysPassValidator<TState>();
        private static readonly Func<IAmAStoredEvent, bool> alwaysPassPredicate = _ => true;

        public EventStoreRepository(IValidateState<TState> stateValidator, IHoldAllConfiguration configs, EventStoreClient client, IDateTimeProvider dateTimeProvider = null)
        {
            this.stateValidator = stateValidator ?? throw new ArgumentNullException(nameof(stateValidator));
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
            this.eventReader = new EventReader(client, configs);
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
        }

        public EventStoreRepository(IHoldAllConfiguration configs,
            EventStoreClient client)
            : this(defaultValidator, configs, client)
        { }

        public async Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists = false, DateTime? appliesAt = null)
        {
            if (throwIfNotExists && !(await Contains(id)))
                throw new StreamNotFoundException(id.ToString());

            var session = new EventStoreSession<TState>(stateValidator, configs, client, id.ToString(), dateTimeProvider);
            await session.Initialize(appliesAt.HasValue
                ? (IAmAStoredEvent e) => GetBeforeDateEventPredicate(e, appliesAt.Value)
                : alwaysPassPredicate);

            return session;
        }

        private bool GetBeforeDateEventPredicate(IAmAStoredEvent @event, DateTime appliesAt)
        {
            if (@event.Metadata?.Properties == null || !@event.Metadata.Properties.TryGetValue(MetadataProperties.Timestamp,
                out var eventTimestamp)) return true;

            if (!DateTime.TryParse(eventTimestamp, out var timestamp)) return true;

            return timestamp <= appliesAt;

        }

        public async Task<bool> Contains(TId selector)
        {
            try
            {
                var readResult = await eventReader.ReadFrom(selector.ToString());

                return readResult.streamExists && await readResult.events.AnyAsync();
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
        [Obsolete("Please use either IEventStoreStreamDeleter.SoftDelete or IEventStoreStreamDeleter.SoftDeleteByEvent")]
        public async Task Delete(TId selector)
        {
            await SoftDelete(selector);
        }

        //[Obsolete("This doesn't care for other operations. In the future it will be moved to session or renamed.")]
        public async Task SoftDelete(TId selector)
        {
            var readResult = await eventReader.ReadFrom(selector.ToString());

            if (readResult.streamExists)
                await client.SoftDeleteAsync(selector.ToString(), StreamState.Any);
        }

        //[Obsolete("This doesn't care for other operations. In the future it will be moved to session or renamed.")]
        public Task SoftDeleteByEvent(TId selector)
            => SoftDeleteByEventImpl(selector, DefaultSoftDeleteEvent.ItemWithType.CreateEventData(dateTimeProvider));

        //[Obsolete("This doesn't care for other operations. In the future it will be moved to session or renamed.")]
        public async Task SoftDeleteByEvent<TSoftDeleteEventType>(TId selector,
            Func<TSoftDeleteEventType> createSoftDeleteEventFunc)
            where TSoftDeleteEventType : EntitySoftDeleted
        {
            if (createSoftDeleteEventFunc == null) throw new ArgumentNullException(nameof(createSoftDeleteEventFunc));

            await SoftDeleteByEventImpl(selector, new ItemWithType(createSoftDeleteEventFunc()).CreateEventData(dateTimeProvider));
        }

        private async Task SoftDeleteByEventImpl(TId selector, EventData softDeleteEvent)
        {
            var readResult = await eventReader.ReadFrom(selector.ToString());

            if (readResult.streamExists)
            {
                await client.AppendToStreamAsync(selector.ToString(), StreamState.Any, new[] {softDeleteEvent});
            }
        }
    }
}
