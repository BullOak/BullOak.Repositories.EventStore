namespace BullOak.Repositories.EventStore.Metadata
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    internal class EventMetadata_V2 : IHoldMetadata
    {
        internal static readonly string TimeStampFormat = "O";
        internal static readonly string MessageIdPropertyName = "message-id";
        internal static readonly string CorrelationIdPropertyName = "correlation-id";
        internal static readonly string CausationIdPropertyName = "causation-id";
        internal static readonly string TimeStampPropertyName = "timestamp";

        public string EventTypeFQN { get; set; }
        public int MetadataVersion { get; set; }

        [JsonIgnore]
        public DateTime TimeStamp
        {
            get => GetTimeStampOrDefault(Properties);
            set
            {
                Properties[TimeStampPropertyName] = value.ToUniversalTime().ToString(TimeStampFormat);
            }
        }
        [JsonIgnore]
        public Guid MessageId
        {
            get => GetGuidOrDefault(Properties, MessageIdPropertyName);
            set => SetGuid(Properties, MessageIdPropertyName, value);
        }
        [JsonIgnore]
        public Guid CorrelationId
        {
            get => GetGuidOrDefault(Properties, CorrelationIdPropertyName);
            set => SetGuid(Properties, CorrelationIdPropertyName, value);
        }
        [JsonIgnore]
        public Guid CausationId
        {
            get => GetGuidOrDefault(Properties, CausationIdPropertyName);
            set => SetGuid(Properties, CausationIdPropertyName, value);
        }

        public Dictionary<string, string> Properties { get; set; }

        public EventMetadata_V2(string eventTypeFQN, Dictionary<string, string> properties)
        {
            EventTypeFQN = eventTypeFQN ?? throw new ArgumentNullException(nameof(EventTypeFQN));
            MetadataVersion = 2;
            Properties = properties;
        }

        internal static EventMetadata_V2 From(ItemWithType @event, params (string key, string value)[] properties)
            => new EventMetadata_V2(@event.type.FullName, properties.ToDictionary(x => x.key, x => x.value));


        public static EventMetadata_V2 Upconvert(EventMetadata_V1 event_V1)
            => new EventMetadata_V2(event_V1.EventTypeFQN, new Dictionary<string, string>());


        private static DateTime GetTimeStampOrDefault(Dictionary<string, string> properties)
        {
            if (properties.TryGetValue(TimeStampPropertyName, out string timestamp))
                return DateTime.Parse(timestamp, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

            return DateTime.MinValue;
        }

        private static Guid GetGuidOrDefault(Dictionary<string, string> properties, string propertyName)
        {
            if (properties.TryGetValue(propertyName, out string guid))
                return Guid.Parse(guid);

            return Guid.Empty;
        }

        private static void SetGuid(Dictionary<string, string> properties, string propertyName, Guid value)
        {
            properties[propertyName] = value.ToString("D");
        }
    }
}
