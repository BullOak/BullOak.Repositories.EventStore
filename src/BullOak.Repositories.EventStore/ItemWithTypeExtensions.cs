namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Globalization;
    using Events;
    using Metadata;
    using Newtonsoft.Json.Linq;
    using StateEmit;
    using EsClientV20 = global::EventStore.Client;
    using EsClientV5 = global::EventStore.ClientAPI;

    public static class ItemWithTypeExtensions
    {
        private static readonly string CanEditJsonFieldName;

        static ItemWithTypeExtensions()
        {
            CanEditJsonFieldName = nameof(ICanSwitchBackAndToReadOnly.CanEdit);
            CanEditJsonFieldName = CanEditJsonFieldName.Substring(0, 1).ToLower()
                                   + CanEditJsonFieldName.Substring(1);
        }

        public static EsClientV20.EventData CreateV20EventData(this ItemWithType @event, IDateTimeProvider dateTimeProvider)
        {
            var metadata = EventMetadata_V2.From(@event,
                (MetadataProperties.Timestamp, dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

            var eventAsJson = JObject.FromObject(@event.instance);
            eventAsJson.Remove(CanEditJsonFieldName);

            return new EsClientV20.EventData(
                EsClientV20.Uuid.NewUuid(),
                @event.type.Name,
                System.Text.Encoding.UTF8.GetBytes(eventAsJson.ToString()),
                MetadataSerializer.Serialize(metadata));
        }

        public static EsClientV5.EventData CreateV5EventData(this ItemWithType @event, IDateTimeProvider dateTimeProvider)
        {
            var metadata = EventMetadata_V2.From(@event,
                (MetadataProperties.Timestamp, dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

            var eventAsJson = JObject.FromObject(@event.instance);
            eventAsJson.Remove(CanEditJsonFieldName);

            return new EsClientV5.EventData(
                Guid.NewGuid(),
                @event.type.Name,
                true,
                System.Text.Encoding.UTF8.GetBytes(eventAsJson.ToString()),
                MetadataSerializer.Serialize(metadata));
        }

        public static bool IsSoftDeleteEvent(this ItemWithType @event)
            => @event.type == DefaultSoftDeleteEvent.Type || @event.type.IsSubclassOf(DefaultSoftDeleteEvent.Type);
    }
}
