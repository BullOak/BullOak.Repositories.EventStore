using BullOak.Repositories.EventStore.Events;
using BullOak.Repositories.EventStore.Metadata;
using EventStore.Client;

namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Streams;


    public class EventStoreReadOnlyRepository<TId, TState> : IReadQueryModels<TId, TState>
    {
        private readonly IHoldAllConfiguration configs;
        private readonly IReadEventsFromStream reader;

        public EventStoreReadOnlyRepository(IHoldAllConfiguration configs, IReadEventsFromStream reader)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
            this.reader = reader;
        }

        public async Task<ReadModel<TState>> ReadFrom(TId id)
        {
            var streamData = await reader.ReadFrom(id.ToString());
            var events = await streamData.Events.Reverse().Select(x=> x.ToItemWithType()).ToArrayAsync();
            var rehydratedState = configs.StateRehydrator.RehydrateFrom<TState>(events);

            //TODO: REPLACE WHEN BO CHANGES TO LONG!!
            return new ReadModel<TState>(rehydratedState, (int)streamData.StoredEventPosition.ToInt64());
        }

        public async Task<TState> ReadFrom(TId id, Func<IAmAStoredEvent, bool> loadEventPredicate)
        {
            var streamData = await reader.ReadFrom(id.ToString(), loadEventPredicate);
            var events = await streamData.Events.Select(x=> x.ToItemWithType()).ToArrayAsync();
            return configs.StateRehydrator.RehydrateFrom<TState>(events);
        }

        public async Task<IEnumerable<ReadModel<TState>>> ReadAllEntitiesFromCategory(string categoryName,
            Func<IAmAStoredEvent, bool> loadEventPredicate = null)
        {
            var streamsData = await reader.ReadFrom($"$ce-{categoryName}", direction: Direction.Forwards, predicate: loadEventPredicate);

            var eventsByStream = await streamsData.Events.GroupBy(x => x.StreamId)
                .SelectAwait(async group =>
                {
                    var events = await group.ToArrayAsync();
                    var state = configs.StateRehydrator.RehydrateFrom<TState>(events.Select(x => x.ToItemWithType()));

                    return (state, currentStreamVersion: events.Length > 0 ? (int) events.Last().PositionInStream : -1);
                })
                .Select(x => new ReadModel<TState>(x.state, x.currentStreamVersion))
                .ToListAsync();

            return eventsByStream;
        }
    }
}
