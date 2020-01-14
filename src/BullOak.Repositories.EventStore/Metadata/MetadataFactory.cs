﻿namespace BullOak.Repositories.EventStore.Metadata
{
    using Newtonsoft.Json.Linq;

    internal class MetadataFactory
    {
        internal static (IHoldMetadata metadata,int version) GetMetadataFrom(int metadataVersion, JObject asJson)
        {
            switch (metadataVersion)
            {
                case 1:
                    return (asJson.ToObject<EventMetadata_V1>(), 1);
                default:
                    throw new MetadataException(asJson, $"Unrecognized Metadata Version: {metadataVersion}");
            }
        }
    }
}
