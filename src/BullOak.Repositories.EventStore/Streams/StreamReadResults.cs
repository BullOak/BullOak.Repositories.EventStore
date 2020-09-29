﻿namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using System.Collections.Generic;

    internal struct StreamReadResults
    {
        public readonly IEnumerable<ItemWithType> events;
        public readonly int streamVersion;

        public StreamReadResults(IEnumerable<ItemWithType> events, int streamVersion)
        {
            this.streamVersion = streamVersion;
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }
    }

    internal struct StreamCategoryReadResults
    {
        public readonly string streamId;
        public readonly IEnumerable<ItemWithType> events;
        public readonly int streamVersion;

        public StreamCategoryReadResults(IEnumerable<ItemWithType> events, string streamId, int streamVersion)
        {
            this.streamVersion = streamVersion;
            this.streamId = streamId;
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }
    }
}
