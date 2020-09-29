namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Streams;

    public class EventStoreReadOnlyRepository<TId, TState> : IReadQueryModels<TId, TState>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly EventReader reader;

        public EventStoreReadOnlyRepository(IHoldAllConfiguration configs, IEventStoreConnection connection)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(connection));

            reader = new EventReader(connection, configs);
        }

        public async Task<ReadModel<TState>> ReadFrom(TId id)
        {
            var streamData = await reader.ReadFrom(id.ToString());
            var rehydratedState = configs.StateRehydrator.RehydrateFrom<TState>(streamData.events);

            return new ReadModel<TState>(rehydratedState, streamData.streamVersion);
        }

        public async Task<TState> ReadFrom(TId id, DateTime appliesAt)
        {
            var streamData = await reader.ReadFrom(id.ToString(), appliesAt);
            return configs.StateRehydrator.RehydrateFrom<TState>(streamData.events);
        }

        public async Task<IEnumerable<ReadModel<TState>>> ReadAllEntitiesFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            var streamsData = await reader.ReadAllEntitiesFromCategory(categoryName, appliesAt);

            return streamsData
                .Select(categoryResult => (configs.StateRehydrator.RehydrateFrom<TState>(categoryResult.events), categoryResult.streamVersion))
                .Select(rehydratedState => new ReadModel<TState>(rehydratedState.Item1, rehydratedState.streamVersion));
        }
    }
}
