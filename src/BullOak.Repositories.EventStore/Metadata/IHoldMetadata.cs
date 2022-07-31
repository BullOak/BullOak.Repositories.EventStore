namespace BullOak.Repositories.EventStore.Metadata
{
    using System;
    using System.Collections.Generic;

    public interface IHoldMetadata
    {
        int MetadataVersion { get; }
        string EventTypeFQN { get; }
        DateTime TimeStamp { get; set; }
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }

        Dictionary<string, string> Properties { get; }
    }
}
