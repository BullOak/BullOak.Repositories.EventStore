namespace BullOak.Repositories.EventStore.Events
{
    using global::EventStore.ClientAPI;
    using Metadata;
    using Newtonsoft.Json;
    using StateEmit;
    using System;
    using System.Linq;

    internal static class EventConversion
    {
        public static (ItemWithType Item, IHoldMetadata Metadata) ToItemWithType(this ResolvedEvent resolvedEvent, ICreateStateInstances stateFactory)
        {
            var serializedEvent = System.Text.Encoding.UTF8.GetString(resolvedEvent.Event.Data);

            var (metadata, type) = ReadTypeFromMetadata(resolvedEvent);

            object @event;

            if (type.IsInterface)
            {
                @event = stateFactory.GetState(type);
                var switchable = @event as ICanSwitchBackAndToReadOnly;

                if(switchable!=null)
                    switchable.CanEdit = true;

                JsonConvert.PopulateObject(serializedEvent, @event);

                if (switchable != null)
                    switchable.CanEdit = false;
            }
            else
                @event = JsonConvert.DeserializeObject(serializedEvent, type);

            return (new ItemWithType(@event, type), metadata);
        }

        private static (IHoldMetadata metadata, Type type) ReadTypeFromMetadata(ResolvedEvent resolvedEvent)
        {
            Type type;
            (IHoldMetadata metadata, int version) metadata;

            if (resolvedEvent.Event.Metadata == null || resolvedEvent.Event.Metadata.Length == 0)
            {
                type = Type.GetType(resolvedEvent.Event.EventType);
                return (null, type);
            }

            metadata = MetadataSerializer.DeserializeMetadata(resolvedEvent.Event.Metadata);
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetType(metadata.metadata.EventTypeFQN))
                .FirstOrDefault(x => x != null);

            return (metadata.metadata, type);
        }
    }
}
