namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.EventStore.Events;
    using BullOak.Repositories.Exceptions;
    using BullOak.Repositories.Repository;
    using BullOak.Repositories.Session;
    using global::EventStore.ClientAPI;
    using System;
    using System.Threading.Tasks;

    public class EventStoreRepository<TId, TState> : IStartSessions<TId, TState>
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
                var eventsTail = await connection.ReadStreamEventsBackwardAsync(
                    id, StreamPosition.End, 1, false);
                if (eventsTail.Events.Length == 0)
                {
                    return false;
                }
                var itemAndType = eventsTail.Events[0].ToItemWithType(configs.StateFactory);
                return eventsTail.Status == SliceReadStatus.Success
                    && itemAndType.type != typeof(SoftDelete);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // TODO Clarify the delete types
        // When ES does a soft-delete the stream will eventually be reclaimed by the Scavenger, so SoftDeleteStream
        // (an ES soft-delete) isn't actually what BullOak considers to be a soft delete. A BullOak soft-delete is
        // achieved by SoftDeleteByEvent via a specific event added to the stream. So, in effect both the hard and soft
        // delete from ES would both be considered hard deletes as far as we are concerned here.
        // Ideally we want to remove Delete from IStartSessions and
        // TODO MS Finish this, also how are we going to do soft/hard delete when the interface only has Delete with no flag?
        // Is this where we need the calling system to publish the BullOakStreamSoftDeleted Alexey mentioned?
        // Or are we changing the IStartSessions interface? Add HardDelete / SoftDelete & mark Delete as obsolete?


        public async Task Delete(TId selector)
        {
            var id = selector.ToString();
            var eventsTail = await connection.ReadStreamEventsBackwardAsync(id, 0, 1, false);
            var expectedVersion = eventsTail.LastEventNumber;
            await connection.DeleteStreamAsync(id, expectedVersion);
        }

        // TODO MS Is it right to use a Session here?
        public async Task SoftDeleteByEvent(TId selector)
        {
            using (var session = await BeginSessionFor(selector))
            {
                session.AddEvent(new SoftDelete());
                await session.SaveChanges();
            }
        }
    }
}
