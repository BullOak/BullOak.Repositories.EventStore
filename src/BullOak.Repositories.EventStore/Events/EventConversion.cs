namespace BullOak.Repositories.EventStore.Events
{
    using Metadata;
    using Newtonsoft.Json;
    using StateEmit;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using global::EventStore.Client;
    using global::EventStore.ClientAPI;

    public static class EventConversion
    {
        public static StoredEvent ToStoredEvent(this EventRecord resolvedEvent, ICreateStateInstances stateFactory)
            => ToStoredEvent(
                resolvedEvent.EventStreamId,
                resolvedEvent.EventNumber.ToInt64(),
                resolvedEvent.Data,
                resolvedEvent.Metadata,
                resolvedEvent.EventType,
                stateFactory
            );

        public static StoredEvent ToStoredEvent(this RecordedEvent resolvedEvent, ICreateStateInstances stateFactory)
            => ToStoredEvent(
                resolvedEvent.EventStreamId,
                resolvedEvent.EventNumber,
                resolvedEvent.Data,
                resolvedEvent.Metadata,
                resolvedEvent.EventType,
                stateFactory
            );

        private static StoredEvent ToStoredEvent(string streamId, long eventNumber, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> meta,
            string eventType, ICreateStateInstances stateFactory)
        {
            var serializedEvent = System.Text.Encoding.UTF8.GetString(data.ToArray());

            var (metadata, type) = ReadTypeFromMetadata(eventType, meta);

            object @event;

            if (type.IsInterface)
            {
                @event = stateFactory.GetState(type);
                var switchable = @event as ICanSwitchBackAndToReadOnly;

                if (switchable != null)
                    switchable.CanEdit = true;

                JsonConvert.PopulateObject(serializedEvent, @event);

                if (switchable != null)
                    switchable.CanEdit = false;
            }
            else
                @event = JsonConvert.DeserializeObject(serializedEvent, type);

            return new StoredEvent(@event, type, streamId, metadata, Convert.ToInt64(eventNumber));
        }

        private static (IHoldMetadata metadata, Type type) ReadTypeFromMetadata(string eventType, ReadOnlyMemory<byte> meta)
        {
            Type type;
            (IHoldMetadata metadata, int version) metadata;

            if (meta.Length == 0)
            {
                type = Type.GetType(eventType);
                return (new EventMetadata_V2(eventType, new Dictionary<string, string>()), type);
            }

            metadata = MetadataSerializer.DeserializeMetadata(meta.ToArray());
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetType(metadata.metadata.EventTypeFQN))
                .FirstOrDefault(x => x != null);

            return (metadata.metadata, type);
        }

        public static ItemWithType ToItemWithType(this StoredEvent se)
            => new(se.DeserializedEvent, se.EventType);
    }
}
