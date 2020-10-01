namespace BullOak.Repositories.EventStore.Streams
{
    internal struct ReadResult
    {
        public ReadResult(string eventStreamId, ItemWithType @event)
        {
            EventStreamId = eventStreamId;
            Event = @event;
        }

        public string EventStreamId { get; }

        public ItemWithType Event { get; }
    }
}
