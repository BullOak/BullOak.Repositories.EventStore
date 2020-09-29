namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using Metadata;

    internal static class MetadataExtensions
    {
        public static bool ShouldInclude(this IHoldMetadata metadata, DateTime appliesAt)
        {
            if (metadata?.Properties == null || !metadata.Properties.TryGetValue(MetadataProperties.Timestamp,
                    out var eventTimestamp)) return true;

            if (!DateTime.TryParse(eventTimestamp, out var timestamp)) return true;

            return timestamp <= appliesAt;
        }
    }
}
