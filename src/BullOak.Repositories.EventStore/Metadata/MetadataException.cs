namespace BullOak.Repositories.EventStore.Metadata
{
    using System;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public class MetadataException : Exception
    {
        private readonly JObject asJson;

        public MetadataException(JObject asJson, string message)
            : base(message)
        {
            this.asJson = asJson;
        }
    }
}