using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using BullOak.Repositories.EventStore;
using BullOak.Repositories.EventStore.Metadata;
using FluentAssertions;

namespace BullOak.Repositories.EventStore.Test.Unit
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class EventMetadataV2Tests
    {
        private static readonly string eventType = typeof(TheEvent).FullName;

        [Fact]
        public void Ctor_WithNoMetadata_ShouldReturnDateTimeMinForTimeStampAndEmptyGuidsForIds()
        {
            var metadata = new EventMetadata_V2(eventType, new Dictionary<string, string>());

            metadata.TimeStamp.Should().Be(DateTime.MinValue);
            metadata.MessageId.Should().Be(Guid.Empty);
            metadata.CausationId.Should().Be(Guid.Empty);
            metadata.CorrelationId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void Ctor_WithOldDateTimeFormat_ShouldParseTimeStampCorrectly()
        {
            var properties = new Dictionary<string, string>();
            var metadata = new EventMetadata_V2(eventType, properties);

            var expectedTimestamp = new DateTime(2022, 1, 30, 12, 13, 14, DateTimeKind.Local).ToUniversalTime();
            properties[EventMetadata_V2.TimeStampPropertyName] = expectedTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var result = metadata.TimeStamp;
            result.Should().Be(expectedTimestamp);

            expectedTimestamp = new DateTime(2022, 7, 30, 12, 13, 14, DateTimeKind.Local).ToUniversalTime();
            properties[EventMetadata_V2.TimeStampPropertyName] = expectedTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
            result = metadata.TimeStamp;
            result.Should().Be(expectedTimestamp);
        }

        [Fact]
        public void Ctor_WithNewTimeStampFormat_ShouldParseTimeStampCorrectly()
        {
            var properties = new Dictionary<string, string>();
            var metadata = new EventMetadata_V2(eventType, properties);

            var expectedTimestamp = new DateTime(2022, 7, 30, 12, 13, 14, DateTimeKind.Local).ToUniversalTime();
            properties[EventMetadata_V2.TimeStampPropertyName] = expectedTimestamp.ToString("O");
            var result = metadata.TimeStamp;
            result.Should().Be(expectedTimestamp);

            expectedTimestamp = new DateTime(2022, 1, 30, 12, 13, 14, DateTimeKind.Local).ToUniversalTime();
            properties[EventMetadata_V2.TimeStampPropertyName] = expectedTimestamp.ToString("O");
            result = metadata.TimeStamp;
            result.Should().Be(expectedTimestamp);
        }

        [Fact]
        public void FromMetadataV1_ShouldNotRaiseExceptions()
        {
            var metadatav1 = new EventMetadata_V1(typeof(TheEvent).FullName);

            var exception = Record.Exception(() => EventMetadata_V2.Upconvert(metadatav1));

            exception.Should().BeNull();
        }

        [Fact]
        public void FromMetadataV1_ShouldHaveCorretEventFQN()
        {
            var metadatav1 = new EventMetadata_V1(typeof(TheEvent).FullName);

            var metadatav2 = EventMetadata_V2.Upconvert(metadatav1);

            metadatav2.EventTypeFQN.Should().Be(metadatav1.EventTypeFQN);
        }

        [Fact]
        public void FromMetadataV1_EventTimeShouldBeDateTimeMinValue()
        {
            var metadatav1 = new EventMetadata_V1(typeof(TheEvent).FullName);

            var metadatav2 = EventMetadata_V2.Upconvert(metadatav1);

            metadatav2.TimeStamp.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void FromMetadtaV1_ShouldHaveEmptyCorrelationCausationAndMessageId()
        {
            var metadatav1 = new EventMetadata_V1(typeof(TheEvent).FullName);

            var metadatav2 = EventMetadata_V2.Upconvert(metadatav1);

            metadatav2.CorrelationId.Should().Be(Guid.Empty);
            metadatav2.CausationId.Should().Be(Guid.Empty);
            metadatav2.MessageId.Should().Be(Guid.Empty);
        }
    }
}
