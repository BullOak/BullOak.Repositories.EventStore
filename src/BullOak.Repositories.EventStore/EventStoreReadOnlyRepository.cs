namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;
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

        public async Task<ReadModel<IEnumerable<TState>>> ReadFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            var streamData = await reader.ReadFromCategory(categoryName, appliesAt);

            var results = new List<ReadModel<IEnumerable<TState>>>();

            foreach (var categoryResult in streamData)
            {
                var rehydratedState = configs.StateRehydrator.RehydrateFrom<TState>(categoryResult.events);
                results.Add(new ReadModel<IEnumerable<TState>>(rehydratedState, streamData.streamVersion));
            }


            return new ReadModel<IEnumerable<TState>>(rehydratedState, streamData.streamVersion);
        }
    }
}
