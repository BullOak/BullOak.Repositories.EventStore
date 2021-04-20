namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    using Appliers;
    using Events;
    using System;

    public class StateApplier : IApplyEvent<IHoldHigherOrder, MyEvent>
        , IApplyEvent<IHoldHigherOrder, IMyEvent>
        , IApplyEvent<IHoldHigherOrder, EntitySoftDeleted>
        , IApplyEvent<IHoldHigherOrder, VisibilityEnabledEvent>
    {
        IHoldHigherOrder IApplyEvent<IHoldHigherOrder, MyEvent>.Apply(IHoldHigherOrder state, MyEvent @event)
            => Apply(state, @event);

        public IHoldHigherOrder Apply(IHoldHigherOrder state, IMyEvent @event)
        {
            state.HigherOrder = Math.Max(@event.Value, state.HigherOrder);

            return state;
        }

        public IHoldHigherOrder Apply(IHoldHigherOrder state, EntitySoftDeleted @event)
            => state;

        public IHoldHigherOrder Apply(IHoldHigherOrder state, VisibilityEnabledEvent @event)
        {
            state.Visibility = true;

            return state;
        }
    }
}
