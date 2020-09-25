namespace BullOak.Repositories.EventStore
{
    using System;
    using Events;
    using global::EventStore.ClientAPI;
    using Metadata;
    using Newtonsoft.Json.Linq;
    using StateEmit;

    public static class ItemWithTypeExtensions
    {
        private static readonly string CanEditJsonFieldName;

        static ItemWithTypeExtensions()
        {
            CanEditJsonFieldName = nameof(ICanSwitchBackAndToReadOnly.CanEdit);
            CanEditJsonFieldName = CanEditJsonFieldName.Substring(0, 1).ToLower()
                                   + CanEditJsonFieldName.Substring(1);
        }

        public static EventData CreateEventData(this ItemWithType @event, IDateTimeProvider dateTimeProvider)
        {
            var metadata = EventMetadata_V2.From(@event, (MetadataProperties.Timestamp, dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

            var eventAsJson = JObject.FromObject(@event.instance);
            eventAsJson.Remove(CanEditJsonFieldName);

            var eventData = new EventData(
                Guid.NewGuid(),
                @event.type.Name,
                true,
                System.Text.Encoding.UTF8.GetBytes(eventAsJson.ToString()),
                MetadataSerializer.Serialize(metadata));
            return eventData;
        }

        public static bool IsSoftDeleteEvent(this ItemWithType @event)
            => @event.type == DefaultSoftDeleteEvent.Type || @event.type.IsSubclassOf(DefaultSoftDeleteEvent.Type);
    }
}
