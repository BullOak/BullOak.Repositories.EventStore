namespace BullOak.Repositories.EventStore.Metadata
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    internal static class MetadataSerializer
    {
        public static readonly Encoding Encoding = Encoding.UTF8;

        public static byte[] Serialize(EventMetadata_V2 metadata)
            => Encoding.GetBytes(JsonConvert.SerializeObject(metadata));

        public static (EventMetadata_V2 metadata, int version) DeserializeMetadata(byte[] metadata)
        {
            var data = Encoding.GetString(metadata);
            var asJson = Newtonsoft.Json.Linq.JObject.Parse(data);

            var metadataVersion = asJson[nameof(EventMetadata_V2.MetadataVersion)].ToObject<int>();

            return MetadataFactory.GetMetadataFrom(metadataVersion, asJson);
        }
    }
}
