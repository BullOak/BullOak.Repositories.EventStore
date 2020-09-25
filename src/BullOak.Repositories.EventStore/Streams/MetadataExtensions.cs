namespace BullOak.Repositories.EventStore.Streams
{
    using System;
    using Metadata;

    internal static class MetadataExtensions
    {
        public static bool ShouldInclude(this IHoldMetadata metadata, DateTime upTo)
        {
            if (metadata.Properties.TryGetValue(MetadataProperties.Timestamp,
                out var eventTimestamp))
            {
                DateTime timestamp;
                if (DateTime.TryParse(eventTimestamp, out timestamp))
                {
                    if (timestamp > upTo) return false;
                }
            }

            return true;
        }
    }
}
