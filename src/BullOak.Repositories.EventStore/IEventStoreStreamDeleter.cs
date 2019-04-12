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
    /// Both a hard-delete and a soft-delete in EventStore result in the stream being removed by the Scavenger, neither
    /// of them correspond to the BullOak soft delete hence we have a new concept <see cref="SoftDeleteByEvent"/>.
    public interface IEventStoreStreamDeleter<TId>
    {
        /// <summary>
        /// Implements a traditional soft-delete i.e. the stream will not be deleted by the scavenger
        /// </summary>
        /// This adds a <see cref="Events.EntitySoftDeleted"/> to the end of the stream and then any events before
        /// this are ignored when returning a session for the stream.
        Task SoftDeleteByEvent(TId selector);

        /// <summary>
        /// Implements a traditional soft-delete using the specified event type
        /// </summary>
        /// <typeparam name="TSoftDeleteEventType">The type to use for the soft delete event</typeparam>
        /// <param name="selector">The ID for the stream</param>
        /// <param name="createSoftDeleteEventFunc">Factory function for the soft delete event</param>
        /// This adds a <typeparamref name="TSoftDeleteEventType"></typeparamref> to the end of the stream and then
        /// any events before this are ignored when returning a session for the stream.
        Task SoftDeleteByEvent<TSoftDeleteEventType>(TId selector, Func<TSoftDeleteEventType> createSoftDeleteEventFunc)
            where TSoftDeleteEventType : EntitySoftDeleted;

        /// <summary>
        /// An EntityStore soft-delete. The stream will eventually be reclaimed by the scavenger.
        /// </summary>
        /// Since the stream will eventually be deleted then this can be considered a hard-delete in BullOak terms.
        Task SoftDelete(TId selector);
    }
}
