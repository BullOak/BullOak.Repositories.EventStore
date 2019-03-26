namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.Exceptions;
    using BullOak.Repositories.Session;
    using global::EventStore.ClientAPI;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BullOak.Repositories.EventStore.Metadata;
    using BullOak.Repositories.StateEmit;
    using Newtonsoft.Json.Linq;

    public class EventStoreSession<TState> : BaseEventSourcedSession<TState, int>
    {
        private static readonly Task<int> done = Task.FromResult(0);
        private readonly IEventStoreConnection eventStoreConnection;
        private readonly string streamName;
        private bool isInDisposedState = false;
        private readonly EventReader eventReader;
        private readonly static string CanEditJsonFieldName;

        static EventStoreSession()
        {
            CanEditJsonFieldName = nameof(ICanSwitchBackAndToReadOnly.CanEdit);
            CanEditJsonFieldName = CanEditJsonFieldName.Substring(0, 1).ToLower()
                                   + CanEditJsonFieldName.Substring(1);
        }

        public EventStoreSession(IHoldAllConfiguration configuration,
                                 IEventStoreConnection eventStoreConnection,
                                 string streamName)
            : base(configuration)
        {
            this.eventStoreConnection = eventStoreConnection ?? throw new ArgumentNullException(nameof(eventStoreConnection));
            this.streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));

            this.eventReader = new EventReader(eventStoreConnection, configuration);
        }

        public async Task Initialize()
        {
            CheckDisposedState();
            //TODO: user credentials
            var streamData = await eventReader.ReadFrom(streamName);
            LoadFromEvents(streamData.events.ToArray(), streamData.streamVersion);
        }

        private void CheckDisposedState()
        {
            if (isInDisposedState)
            {
                //this is purely design decision, nothing prevents implementing the session that support any amount and any order of oeprations
                throw new InvalidOperationException("EventStoreSession should not be used after SaveChanges call");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { ConsiderSessionDisposed(); }
            base.Dispose(disposing);
        }

        private void ConsiderSessionDisposed()
        {
            isInDisposedState = true;
        }

        /// <summary>
        /// Saves changes to the respective stream
        /// NOTES: Current implementation doesn't support cancellation token
        /// </summary>
        /// <param name="eventsToAdd"></param>
        /// <param name="snapshot"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<int> SaveChanges(ItemWithType[] eventsToAdd,
            TState snapshot,
            CancellationToken? cancellationToken)
        {
            checked
            {
                CheckDisposedState();
                ConditionalWriteResult writeResult;

                writeResult = await eventStoreConnection.ConditionalAppendToStreamAsync(
                        streamName,
                        this.ConcurrencyId,
                        eventsToAdd.Select(eventObject => CreateEventData(eventObject)))
                    .ConfigureAwait(false);

                switch (writeResult.Status)
                {
                    case ConditionalWriteStatus.Succeeded:
                        break;
                    case ConditionalWriteStatus.VersionMismatch:
                        throw new ConcurrencyException(streamName, null);
                    case ConditionalWriteStatus.StreamDeleted:
                        throw new InvalidOperationException($"Stream was deleted. StreamId: {streamName}");
                    default:
                        throw new InvalidOperationException($"Unexpected write result: {writeResult.Status}");
                }

                if (!writeResult.NextExpectedVersion.HasValue)
                {
                    throw new InvalidOperationException("Eventstore data write outcome unexpected. NextExpectedVersion is null");
                }

                await Initialize();
                ConsiderSessionDisposed();
                return (int)writeResult.NextExpectedVersion.Value;
            }
        }

        private EventData CreateEventData(ItemWithType @event)
        {
            var metadata = EventMetadata_V1.From(@event);

            var eventAsJson = JObject.FromObject(@event.instance);
            eventAsJson.Remove(CanEditJsonFieldName);

            var eventData = new EventData(
                Guid.NewGuid(),
                @event.type.Name,
                true,
                System.Text.Encoding.UTF8.GetBytes(eventAsJson.ToString()),
                MetadataSerializer.Serialize(metadata));
            return eventData;
        }
    }
}
