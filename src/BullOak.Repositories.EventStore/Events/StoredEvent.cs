﻿using System;
using BullOak.Repositories.EventStore.Metadata;

namespace BullOak.Repositories.EventStore.Events
{
    public record struct StoredEvent(object DeserializedEvent, Type EventType, string StreamId, IHoldMetadata Metadata, long PositionInStream) : IAmAStoredEvent;
}
