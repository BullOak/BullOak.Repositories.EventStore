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
            var streamData = await reader.ReadFrom(id.ToString(), direction: StreamReadDirection.Forwards);
            var events = streamData.Events.Select(x=> x.ToItemWithType());
            var rehydratedState = await configs.StateRehydrator.RehydrateFrom<TState>(events);

            //TODO: REPLACE WHEN BO CHANGES TO LONG!!
            return new ReadModel<TState>(rehydratedState, (int)streamData.StoredEventPosition.ToInt64());
        }

        public async Task<TState> ReadFrom(TId id, Func<IAmAStoredEvent, bool> loadEventPredicate)
        {
            var streamData = await reader.ReadFrom(id.ToString(), predicate: loadEventPredicate);

            var events = streamData.Events.Select(x=> x.ToItemWithType());
            return await configs.StateRehydrator.RehydrateFrom<TState>(events);
        }

        public async Task<IEnumerable<ReadModel<TState>>> ReadAllEntitiesFromCategory(string categoryName,
            Func<IAmAStoredEvent, bool> loadEventPredicate = null)
        {
            var streamsData = await reader.ReadFrom($"$ce-{categoryName}", predicate: loadEventPredicate, StreamReadDirection.Forwards);
            var states = new Dictionary<string, TState>();
            var lastIndexSeen = new Dictionary<string, long>();
            var stateType = typeof(TState);

            await foreach(var @event in streamsData.Events)
            {
                var state = (TState)GetOrCreateFrom(@event, states);

                state = (TState)configs.EventApplier.ApplyEvent(stateType, state, @event.ToItemWithType());

                states[@event.StreamId] = state;
                lastIndexSeen[@event.StreamId] = @event.PositionInStream;
            }

            return from key in states.Keys
                   select new ReadModel<TState>(states[key], (int)lastIndexSeen[key]);
        }

        private object GetOrCreateFrom(StoredEvent @event, Dictionary<string, TState> states)
        {
            if (!states.TryGetValue(@event.StreamId, out TState state))
            {
                state = (TState)configs.StateFactory.GetState(typeof(TState));
                states[@event.StreamId] = state;
            }

            return state;
        }
    }
}
