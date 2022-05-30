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
    using System.Collections.Concurrent;

    public static class EventConversion
    {
        private static ConcurrentDictionary<string, Type> TypeRegistry { get; } = new ConcurrentDictionary<string, Type>();

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

        public static StoredEvent ToStoredEvent(string streamId, long eventNumber, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> meta,
            string eventTypeName, ICreateStateInstances stateFactory)
        {
            var serializedEvent = System.Text.Encoding.UTF8.GetString(data.Span);

            var metadata = LoadMetadata(eventTypeName, meta);
            var eventType = LoadType(metadata);

            object @event;

            if (eventType.IsInterface)
            {
                @event = stateFactory.GetState(eventType);
                var switchable = @event as ICanSwitchBackAndToReadOnly;

                if (switchable != null)
                    switchable.CanEdit = true;

                JsonConvert.PopulateObject(serializedEvent, @event);

                if (switchable != null)
                    switchable.CanEdit = false;
            }
            else
                @event = JsonConvert.DeserializeObject(serializedEvent, eventType);

            return new StoredEvent(@event, eventType, streamId, metadata, Convert.ToInt64(eventNumber));
        }

        private static IHoldMetadata LoadMetadata(string eventType, ReadOnlyMemory<byte> meta)
            => meta.Length == 0 ?
                new EventMetadata_V2(eventType, new Dictionary<string, string>())
                : MetadataSerializer.DeserializeMetadata(meta).metadata;

        private static Type LoadType(IHoldMetadata metadata)
            => TypeRegistry.GetOrAdd(metadata.EventTypeFQN, LoadTypeByName);

        private static Type LoadTypeByName(string typeFQN)
        {
            //Try to find it from the loaded assemblies. This is needed for emitted types
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetType(typeFQN))
                .FirstOrDefault(x => x != null);

            //The above will fail for generic types as it's very basic. In this case just try to load it by name
            if (type == null)
                type = Type.GetType(typeFQN);

            return type ?? throw new TypeNotFoundException(typeFQN);
        }

        public static ItemWithType ToItemWithType(this StoredEvent se)
            => new(se.DeserializedEvent, se.EventType);
    }
}
