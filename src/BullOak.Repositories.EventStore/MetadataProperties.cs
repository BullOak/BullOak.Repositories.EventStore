using System;

namespace BullOak.Repositories.EventStore
{
    [Obsolete("Please use the properties in each Metadata class you're interested in. Changes in these names are breaking changes to the metadata classes so they should be treaded as such and be part of their contract,")]
    public static class MetadataProperties
    {
        [Obsolete("Please use the properties in each Metadata class you're interested in. Changes in these names are breaking changes to the metadata classes so they should be treaded as such and be part of their contract,")]
        public static readonly string Timestamp = "timestamp";
    }
}
