using System;
using System.Collections.Generic;
using BullOak.Repositories.EventStore.Metadata;
using EventStore.Client;

namespace BullOak.Repositories.EventStore.Events
{
    public interface IAmAStoredEvent
    {
        object DeserializedEvent { get; }
        Type EventType { get; }
        string StreamId { get; }
        IHoldMetadata Metadata { get; }
        long PositionInStream { get; }

        ItemWithType ToItemWithType() =>
            new ItemWithType(DeserializedEvent, EventType);
    }
}
