namespace BullOak.Repositories.EventStore.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class EventMetadata_V2 : IHoldMetadata
    {
        public string EventTypeFQN { get; set; }
        public int MetadataVersion { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public EventMetadata_V2(string eventTypeFQN, params (string key, string value)[] properties)
        {
            EventTypeFQN = eventTypeFQN ?? throw new ArgumentNullException(nameof(EventTypeFQN));
            MetadataVersion = 2;
            Properties = properties.ToDictionary(x => x.key, x => x.value);
        }

        internal static EventMetadata_V2 From(ItemWithType @event, params (string key, string value)[] properties)
            => new EventMetadata_V2(@event.type.FullName, properties);

        public static EventMetadata_V2 Upconvert(EventMetadata_V1 event_V1)
        {
            return new EventMetadata_V2(event_V1.EventTypeFQN);
        }
    }
}
