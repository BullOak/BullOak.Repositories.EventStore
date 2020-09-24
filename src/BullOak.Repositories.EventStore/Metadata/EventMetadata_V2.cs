namespace BullOak.Repositories.EventStore.Metadata
{
    using System;

    internal class EventMetadata_V2
    {
        public string EventTypeFQN { get; set; }
        public DateTime AsOf { get; set; }
        public int MetadataVersion { get; set; }

        public EventMetadata_V2(string eventTypeFQN)
        {
            EventTypeFQN = eventTypeFQN ?? throw new ArgumentNullException(nameof(EventTypeFQN));
            MetadataVersion = 2;
            AsOf = DateTime.UtcNow;
        }

        internal static EventMetadata_V2 From(ItemWithType @event)
            => new EventMetadata_V2(@event.type.FullName);

        public static EventMetadata_V2 Upconvert(EventMetadata_V1 event_V1)
        {
            return new EventMetadata_V2(event_V1.EventTypeFQN);
        }
    }
}
