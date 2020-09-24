namespace BullOak.Repositories.EventStore.Metadata
{
    using Newtonsoft.Json.Linq;

    internal class MetadataFactory
    {
        internal static (IHoldMetadata metadata,int version) GetMetadataFrom(int metadataVersion, JObject asJson)
        {
            switch (metadataVersion)
            {
                case 1:
                    var eventV1 = (asJson.ToObject<EventMetadata_V1>(), 1);
                    return (EventMetadata_V2.Upconvert(eventV1.Item1), 2);
                case 2:
                    return (asJson.ToObject<EventMetadata_V2>(), 2);
                default:
                    throw new MetadataException(asJson, $"Unrecognized Metadata Version: {metadataVersion}");
            }
        }
    }
}
