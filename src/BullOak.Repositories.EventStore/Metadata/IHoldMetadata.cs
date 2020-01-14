namespace BullOak.Repositories.EventStore.Metadata
{
    internal interface IHoldMetadata
    {
        int MetadataVersion { get; set; }
        string EventTypeFQN { get; set; }
    }
}
