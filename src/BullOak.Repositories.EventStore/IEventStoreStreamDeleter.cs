namespace BullOak.Repositories.EventStore
{
    using System;
    using System.Threading.Tasks;
    using Events;

    /// <summary>
    /// Interface to capture the concept by which BullOak's Hard and Soft deletes are mapped to deletes in EventStore
    /// </summary>
    /// <typeparam name="TId">The type of the ID used to select the stream</typeparam>
    /// In BullOak a hard-delete should remove the stream, whereas a soft-delete should leave the stream to read in
    /// future from EventStore but treat the stream as deleted for the purpose of returning sessions to it.
    public interface IEventStoreStreamDeleter<TId>
    {
        /// <summary>
        /// An EntityStore soft-delete. The stream will eventually be reclaimed by the scavenger.
        /// </summary>
        /// Since the stream will eventually be deleted then this can be considered a hard-delete in BullOak terms.
        Task SoftDelete(TId selector);
    }
}
