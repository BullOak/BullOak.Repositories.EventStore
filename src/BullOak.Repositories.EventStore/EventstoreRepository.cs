﻿namespace BullOak.Repositories.EventStore
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
                var eventsTail = await GetLastEvent(id);

                if (eventsTail.Status != SliceReadStatus.Success)
                {
                    return false;
                }

                // If the last event is a soft delete then we consider the stream to not exist
                if (eventsTail.Events.Length > 0)
                {
                    var @event = eventsTail.Events[0].ToItemWithType(configs.StateFactory);
                    return !@event.IsSoftDeleteEvent();
                }

                return true;
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

        public async Task SoftDelete(TId selector)
        {
            var id = selector.ToString();
            var expectedVersion = await GetLastEventNumber(id);
            await connection.DeleteStreamAsync(id, expectedVersion);
        }

        public Task SoftDeleteByEvent(TId selector)
            => SoftDeleteByEventImpl(selector, DefaultSoftDeleteEvent.ItemWithType.CreateEventData());

        public async Task SoftDeleteByEvent<TSoftDeleteEventType>(TId selector,
            Func<TSoftDeleteEventType> createSoftDeleteEventFunc)
            where TSoftDeleteEventType : EntitySoftDeleted
        {
            if (createSoftDeleteEventFunc == null) throw new ArgumentNullException(nameof(createSoftDeleteEventFunc));

            await SoftDeleteByEventImpl(selector, new ItemWithType(createSoftDeleteEventFunc()).CreateEventData());
        }

        private async Task SoftDeleteByEventImpl(TId selector, EventData softDeleteEvent)
        {
            var id = selector.ToString();

            var expectedVersion = await GetLastEventNumber(id);
            var writeResult = await connection.ConditionalAppendToStreamAsync(
                    id,
                    expectedVersion,
                    new[] { softDeleteEvent })
                .ConfigureAwait(false);

            StreamAppendHelpers.CheckConditionalWriteResultStatus(writeResult, id);
        }

        private async Task<long> GetLastEventNumber(string id)
        {
            var eventsTail = await GetLastEvent(id);
            return eventsTail.LastEventNumber;
        }

        private Task<StreamEventsSlice> GetLastEvent(string id)
            => connection.ReadStreamEventsBackwardAsync(id, StreamPosition.End, 1, false);
    }
}
