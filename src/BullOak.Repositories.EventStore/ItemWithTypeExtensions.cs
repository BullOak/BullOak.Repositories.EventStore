﻿using EventStore.Client;
using System;
using BullOak.Repositories.EventStore.Events;
using BullOak.Repositories.EventStore.Metadata;
using Newtonsoft.Json.Linq;
using BullOak.Repositories.StateEmit;


namespace BullOak.Repositories.EventStore
{
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
            var metadata = EventMetadata_V2.From(@event,
                (MetadataProperties.Timestamp, dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));

            var eventAsJson = JObject.FromObject(@event.instance);
            eventAsJson.Remove(CanEditJsonFieldName);

            return new EventData(
                Uuid.NewUuid(),
                @event.type.Name,
                System.Text.Encoding.UTF8.GetBytes(eventAsJson.ToString()),
                MetadataSerializer.Serialize(metadata));
        }

        public static bool IsSoftDeleteEvent(this ItemWithType @event)
            => @event.type == DefaultSoftDeleteEvent.Type || @event.type.IsSubclassOf(DefaultSoftDeleteEvent.Type);
    }
}
