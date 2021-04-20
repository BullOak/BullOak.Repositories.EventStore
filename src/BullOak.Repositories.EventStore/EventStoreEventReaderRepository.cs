namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.EventStore.Streams;
    using global::EventStore.ClientAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventStoreEventReaderRepository<TId> : IReadEvents<TId>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly EventReader reader;

        public EventStoreEventReaderRepository(IHoldAllConfiguration configs, IEventStoreConnection connection)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));

            reader = new EventReader(connection, configs);
        }

        public async Task<IEnumerable<ItemWithType>> ReadFrom(TId id)
        {
            var streamData = await reader.ReadFrom(new ReadStreamBackwardsStrategy(id.ToString()));

            return streamData.results.Select(x => x.Event); ;
        }
    }
}
