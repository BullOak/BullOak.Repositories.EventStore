﻿using BullOak.Repositories.EventStore.Events;

namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IReadQueryModels<in TId, TState>
    {
        Task<ReadModel<TState>> ReadFrom(TId id);
        Task<TState> ReadFrom(TId id, Func<StoredEvent, bool> predicate);

        Task<IEnumerable<ReadModel<TState>>> ReadAllEntitiesFromCategory(string categoryName,
            Func<StoredEvent, bool> predicate = null);
    }
}
