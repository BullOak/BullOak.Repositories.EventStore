namespace BullOak.Repositories.EventStore.Metadata
{
    using System.Collections.Generic;

    public interface IHoldMetadata
    {
        int MetadataVersion { get; }
        string EventTypeFQN { get; }
        Dictionary<string, string> Properties { get; }
    }
}
