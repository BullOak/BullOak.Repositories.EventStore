namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.Session;
    using global::EventStore.ClientAPI;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Streams;

    public class EventStoreSession<TState> : BaseEventSourcedSession<TState, int>
    {
        private static readonly Task<int> done = Task.FromResult(0);

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IKeepESConnectionAlive esConnection;
        private readonly string streamName;
        private bool isInDisposedState = false;
        private readonly EventReader eventReader;
        private static readonly IValidateState<TState> defaultValidator = new AlwaysPassValidator<TState>();

        private IEventStoreConnection ESConnection => esConnection.Connection;

        public EventStoreSession(IHoldAllConfiguration configuration,
            IKeepESConnectionAlive esConnection,
            string streamName,
            IDateTimeProvider dateTimeProvider = null)
            : this(defaultValidator, configuration, esConnection, streamName, dateTimeProvider)
        { }

        public EventStoreSession(IHoldAllConfiguration configuration,
            IEventStoreConnection connection,
            string streamName,
            IDateTimeProvider dateTimeProvider = null)
            : this(defaultValidator, configuration, new EventStoreConnectionContainer(connection), streamName, dateTimeProvider)
        { }

        public EventStoreSession(IValidateState<TState> stateValidator,
            IHoldAllConfiguration configuration,
            IKeepESConnectionAlive esConnection,
            string streamName,
            IDateTimeProvider dateTimeProvider = null)
            : base(stateValidator, configuration)
        {
            this.streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
            this.esConnection = esConnection ?? throw new ArgumentNullException(nameof(esConnection));

            this.eventReader = new EventReader(esConnection, configuration);
        }

        public async Task Initialize(DateTime? appliesAt = null)
        {
            CheckDisposedState();
            //TODO: user credentials
            var streamData = await eventReader.ReadFrom(new ReadStreamBackwardsStrategy(streamName), appliesAt);
            LoadFromEvents(streamData.results.Select(x => x.Event).ToArray(), streamData.streamVersion);
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

                writeResult = await ESConnection.ConditionalAppendToStreamAsync(
                        streamName,
                        this.ConcurrencyId,
                        eventsToAdd.Select(eventObject => eventObject.CreateEventData(dateTimeProvider)))
                    .ConfigureAwait(false);

                StreamAppendHelpers.CheckConditionalWriteResultStatus(writeResult, streamName);

                if (!writeResult.NextExpectedVersion.HasValue)
                {
                    throw new InvalidOperationException("Eventstore data write outcome unexpected. NextExpectedVersion is null");
                }

                //TODO: is this necessary?? All tests still pass with it removed
                //await Initialize();

                ConsiderSessionDisposed();
                return (int)writeResult.NextExpectedVersion.Value;
            }
        }
    }
}
