namespace BullOak.Repositories.EventStore.Metadata
{
    using System;

    internal class EventMetadata_V1
    {
        public string EventTypeFQN { get; set; }
        public int MetadataVersion { get; set; }

        public EventMetadata_V1(string eventTypeFQN)
        {
            EventTypeFQN = eventTypeFQN ?? throw new ArgumentNullException(nameof(EventTypeFQN));
            MetadataVersion = 1;
        }
    }
}
