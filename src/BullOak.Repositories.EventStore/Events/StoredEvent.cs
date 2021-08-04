using System;
using BullOak.Repositories.EventStore.Metadata;

namespace BullOak.Repositories.EventStore.Events
{
    internal record StoredEvent(object DeserializedEvent, Type EventType, string StreamId, IHoldMetadata Metadata, long PositionInStream) : IAmAStoredEvent;
}
