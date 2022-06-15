using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using BullOak.Repositories.EventStore.Metadata;
using BullOak.Repositories.StateEmit;
using Newtonsoft.Json;
using BullOak.Repositories.EventStore.Events;
using FluentAssertions;

namespace BullOak.Repositories.EventStore.Test.Unit
{
    public class TheEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    internal class APrivateEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class AGenericEvent<T>
    {
        public T TheValue { get; set; }
    }

    public class InstanceCreatorStub : ICreateStateInstances
    {
        private static InstanceCreatorStub instance = new InstanceCreatorStub();
        public static InstanceCreatorStub Instance => instance;

        public object GetState(Type type)
        {
            if (type == typeof(TheEvent))
                return new TheEvent();

            throw new NotSupportedException();
        }

        public Func<T, T> GetWrapper<T>()
            => t => t;

        public void WarmupWith(IEnumerable<Type> typesToCreateFactoriesFor)
        { }
    }

    public class EventConversionTests
    {
        private struct EventAndMetadata<T>
        {
            public T Event => (T)ItemWithType.instance;
            public ItemWithType ItemWithType { get; private set; }
            public byte[] Metadata { get; private set; }
            public byte[] EventData => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Event));

            public EventAndMetadata(ItemWithType itemWithType, byte[] metadata)
            {
                ItemWithType = itemWithType;
                Metadata = metadata;
            }
        }

        private static EventAndMetadata<T> GetFromType<T>(bool addMetadata)
        {
            var instance = Activator.CreateInstance(typeof(T));
            var itemWithType = new ItemWithType(instance);
            var metadata = addMetadata ? MetadataSerializer.Serialize(EventMetadata_V2.From(itemWithType)) : null;

            return new EventAndMetadata<T>(itemWithType, metadata);
        }

        private static StoredEvent CallConvertToStoreEventWith<T>(EventAndMetadata<T> eventAndMeta)
            => EventConversion.ToStoredEvent("streamId", 5, eventAndMeta.EventData, eventAndMeta.Metadata,
                eventAndMeta.ItemWithType.type.FullName,
                InstanceCreatorStub.Instance);

        [Fact]
        public void EventConversion_NoMetadataPublicClass_IsDiscovered()
        {
            var eventAndMeta = GetFromType<TheEvent>(addMetadata: false);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(TheEvent));
        }

        [Fact]
        public void EventConversion_MetadataWithFQNOfPublicClass_IsDiscovered()
        {
            var eventAndMeta = GetFromType<TheEvent>(addMetadata: true);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(TheEvent));
        }

        [Fact]
        public void EventConversion_NoMetadataInternalClass_IsDiscovered()
        {
            var eventAndMeta = GetFromType<APrivateEvent>(addMetadata: false);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(APrivateEvent));
        }

        [Fact]
        public void EventConversion_MetadataWithFQNOfInternalClass_IsDiscovered()
        {
            var eventAndMeta = GetFromType<APrivateEvent>(addMetadata: true);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(APrivateEvent));
        }

        [Fact]
        public void EventConversion_GenericPublicClassAndMetadata_IsDiscovered()
        {
            var eventAndMeta = GetFromType<AGenericEvent<TheEvent>>(addMetadata: true);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(AGenericEvent<TheEvent>));
        }

        [Fact]
        public void EventConversion_GenericPublicClassButNoMetadata_IsDiscovered()
        {
            var eventAndMeta = GetFromType<AGenericEvent<TheEvent>>(addMetadata: false);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(AGenericEvent<TheEvent>));
        }

        [Fact]
        public void EventConversion_GenericPublicClassWithInternalGenericAttributeAndMetadata_IsDiscovered()
        {
            var eventAndMeta = GetFromType<AGenericEvent<APrivateEvent>>(addMetadata: true);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(AGenericEvent<APrivateEvent>));
        }

        [Fact]
        public void EventConversion_GenericPublicClassWithInternalGenericAttributeAndNoMetadata_IsDiscovered()
        {
            var eventAndMeta = GetFromType<AGenericEvent<APrivateEvent>>(addMetadata: false);

            var storedEvent = CallConvertToStoreEventWith(eventAndMeta);

            storedEvent.EventType.Should().NotBeNull();
            storedEvent.EventType.Should().Be(typeof(AGenericEvent<APrivateEvent>));
        }

        [Fact]
        public void EventConversion_GenericPublicClassWithUnloadedGenericAttribute_IsDiscovered()
        {
            var fqn = "BullOak.Repositories.EventStore.Test.Unit.AGenericEvent`1[[BullOak.Repositories.EventStore.TestingExtras.SuperHiddenType, BullOak.Repositories.EventStore.TestingExtras, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null]]";
            var @event = new AGenericEvent<TheEvent>(); // Used for serialization purposes
            var eventWithType = new ItemWithType(@event);
            byte[] metadataBytes = MetadataSerializer.Serialize(new EventMetadata_V2(fqn, new Dictionary<string, string>()));
            var eventBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

            var storedEvent = EventConversion.ToStoredEvent("streamId", 5, eventBytes, metadataBytes,
                eventWithType.type.FullName,
                InstanceCreatorStub.Instance);

            storedEvent.EventType.Should().NotBeNull();
        }

        [Fact]
        public void EventConversion_GenericPublicClassWithUnloadedGenericAttributeAndWrongAssemblyVersion_IgnoresAssemblyVersionAndIsDiscovered()
        {
            var fqn = "BullOak.Repositories.EventStore.Test.Unit.AGenericEvent`1[[BullOak.Repositories.EventStore.TestingExtras.SuperHiddenType, BullOak.Repositories.EventStore.TestingExtras, Version = 22.2.6643.0, Culture = neutral, PublicKeyToken = null]]";
            var @event = new AGenericEvent<TheEvent>(); // Used for serialization purposes
            var eventWithType = new ItemWithType(@event);
            byte[] metadataBytes = MetadataSerializer.Serialize(new EventMetadata_V2(fqn, new Dictionary<string, string>()));
            var eventBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

            var storedEvent = EventConversion.ToStoredEvent("streamId", 5, eventBytes, metadataBytes,
                fqn,
                InstanceCreatorStub.Instance);

            storedEvent.EventType.Should().NotBeNull();
        }

        [Fact]
        public void EventConversion_GenericPublicClassWithNonExistingGenericType_CompletesWithoutFindingEventType()
        {
            var fqn = "BullOak.Repositories.EventStore.Test.Unit.AGenericEvent`1[[BullOak.Repositories.EventStore.TestingExtras.NonExistingType, BullOak.Repositories.EventStore.TestingExtras, Version = 22.2.6643.0, Culture = neutral, PublicKeyToken = null]]";
            var @event = new AGenericEvent<TheEvent>(); // Used for serialization purposes
            var eventWithType = new ItemWithType(@event);
            byte[] metadataBytes = MetadataSerializer.Serialize(new EventMetadata_V2(fqn, new Dictionary<string, string>()));
            var eventBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

            StoredEvent storedEvent = null;
            var exception = Record.Exception(() => storedEvent = EventConversion.ToStoredEvent("streamId", 5, eventBytes, metadataBytes,
                fqn,
                InstanceCreatorStub.Instance));

            storedEvent.Should().BeNull();
            exception.Should().BeOfType<TypeNotFoundException>();
        }

    }
}
