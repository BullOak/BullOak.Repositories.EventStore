﻿using BullOak.Repositories.EventStore.Events;

namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    using BullOak.Repositories.Appliers;
    using System;

    public class StateApplier : IApplyEvent<IHoldHigherOrder, MyEvent>
        , IApplyEvent<IHoldHigherOrder, IMyEvent>
        , IApplyEvent<IHoldHigherOrder, SoftDelete>
    {
        IHoldHigherOrder IApplyEvent<IHoldHigherOrder, MyEvent>.Apply(IHoldHigherOrder state, MyEvent @event)
            => Apply(state, @event);

        public IHoldHigherOrder Apply(IHoldHigherOrder state, IMyEvent @event)
        {
            state.HigherOrder = Math.Max(@event.Value, state.HigherOrder);

            return state;
        }

        public IHoldHigherOrder Apply(IHoldHigherOrder state, SoftDelete @event)
        {
            state.SoftDeleteFound = true;

            return state;
        }
    }
}
