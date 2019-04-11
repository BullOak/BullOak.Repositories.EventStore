namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.EventStore.Events;
    using BullOak.Repositories.Exceptions;
    using BullOak.Repositories.Repository;
    using BullOak.Repositories.Session;
    using global::EventStore.ClientAPI;
    using System;
    using System.Threading.Tasks;

    public class EventStoreRepository<TId, TState> : IStartSessions<TId, TState>, IEventStoreStreamDeleter<TId>
    {
        private static readonly Task<bool> falseResult = Task.FromResult(false);
        private static readonly ItemWithType softDeleteEvent = new ItemWithType(new SoftDeleteEvent());
        private readonly IHoldAllConfiguration configs;
        private readonly IEventStoreConnection connection;

        public EventStoreRepository(IHoldAllConfiguration configs, IEventStoreConnection connection)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(connection));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<IManageSessionOf<TState>> BeginSessionFor(TId id, bool throwIfNotExists = false)
        {
            if (throwIfNotExists && !(await Contains(id)))
                throw new StreamNotFoundException(id.ToString());

            var session = new EventStoreSession<TState>(configs, connection, id.ToString());
            await session.Initialize();

            return session;
        }

        public async Task<bool> Contains(TId selector)
        {
            try
            {
                var id = selector.ToString();
                var eventsTail = await connection.ReadStreamEventsBackwardAsync(
                    id, StreamPosition.End, 1, false);
                if (eventsTail.Events.Length == 0)
                {
                    return false;
                }
                var itemAndType = eventsTail.Events[0].ToItemWithType(configs.StateFactory);
                return eventsTail.Status == SliceReadStatus.Success
                    && itemAndType.type != typeof(SoftDeleteEvent);
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
        /// you want proper soft-delete semantics then use <see cref="IEventStoreStreamDeleter{TId}.SoftDeleteByEvent"/>
        [Obsolete("Please use either IEventStoreStreamDeleter.SoftDelete or IEventStoreStreamDeleter.SoftDeleteByEvent")]
        public async Task Delete(TId selector)
        {
            await SoftDelete(selector);
        }

        public async Task SoftDeleteByEvent(TId selector)
        {
            var id = selector.ToString();

            var expectedVersion = await GetLastEventNumber(id);
            var writeResult = await connection.ConditionalAppendToStreamAsync(
                id,
                expectedVersion,
                new[] {softDeleteEvent.CreateEventData()})
                .ConfigureAwait(false);

            StreamAppendHelpers.CheckConditionalWriteResultStatus(writeResult, id);
        }

        public async Task SoftDelete(TId selector)
        {
            var id = selector.ToString();
            var expectedVersion = await GetLastEventNumber(id);
            await connection.DeleteStreamAsync(id, expectedVersion);
        }

        private async Task<long> GetLastEventNumber(string id)
        {
            var eventsTail = await connection.ReadStreamEventsBackwardAsync(id, 0, 1, false);
            return eventsTail.LastEventNumber;
        }
    }
}
