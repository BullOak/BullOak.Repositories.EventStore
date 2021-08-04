using System.Collections.Generic;
using EventStore.Client;

namespace BullOak.Repositories.EventStore.Events
{
    using Metadata;
    using Newtonsoft.Json;
    using StateEmit;
    using System;
    using System.Linq;

    internal static class EventConversion
    {
        public static StoredEvent ToStoredEvent(this EventRecord resolvedEvent,
            ICreateStateInstances stateFactory)
        {
            var serializedEvent = System.Text.Encoding.UTF8
                .GetString(resolvedEvent.Data.Span);
            
            var (metadata, type) = ReadTypeFromMetadata(resolvedEvent);

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

            return new StoredEvent(@event, type, resolvedEvent.EventStreamId, metadata,
                resolvedEvent.EventNumber.ToInt64());
        }

        public static ItemWithType ToItemWithType(this StoredEvent se)
            => new ItemWithType(se.DeserializedEvent, se.EventType);

        private static (IHoldMetadata metadata, Type type) ReadTypeFromMetadata(EventRecord resolvedEvent)
        {
            Type type;
            (IHoldMetadata metadata, int version) metadata;

            if (resolvedEvent.Metadata.IsEmpty)
            {
                type = Type.GetType(resolvedEvent.EventType);
                return (new EventMetadata_V2(resolvedEvent.EventType, new Dictionary<string, string>()), type);
            }

            metadata = MetadataSerializer.DeserializeMetadata(resolvedEvent.Metadata.ToArray());
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetType(metadata.metadata.EventTypeFQN))
                .FirstOrDefault(x => x != null);

            return (metadata.metadata, type);
        }
    }
}
