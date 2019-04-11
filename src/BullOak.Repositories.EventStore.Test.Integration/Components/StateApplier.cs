namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    using BullOak.Repositories.Appliers;
    using BullOak.Repositories.EventStore.Events;
    using System;

    public class StateApplier : IApplyEvent<IHoldHigherOrder, MyEvent>
        , IApplyEvent<IHoldHigherOrder, IMyEvent>
        , IApplyEvent<IHoldHigherOrder, SoftDeleteEvent>
    {
        IHoldHigherOrder IApplyEvent<IHoldHigherOrder, MyEvent>.Apply(IHoldHigherOrder state, MyEvent @event)
            => Apply(state, @event);

        public IHoldHigherOrder Apply(IHoldHigherOrder state, IMyEvent @event)
        {
            state.HigherOrder = Math.Max(@event.Value, state.HigherOrder);

            return state;
        }

        public IHoldHigherOrder Apply(IHoldHigherOrder state, SoftDeleteEvent @event)
            => state;
    }
}
