namespace BullOak.Repositories.EventStore.Metadata
{
    using System.Collections.Generic;

    internal interface IHoldMetadata
    {
        int MetadataVersion { get; set; }
        string EventTypeFQN { get; set; }
        Dictionary<string, string> Properties { get; set; }
    }
}
