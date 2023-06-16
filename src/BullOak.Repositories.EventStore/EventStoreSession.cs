namespace BullOak.Repositories.EventStore
{
    using Session;
    using Events;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Streams;

    public class EventStoreSession<TState> : BaseEventSourcedSession<TState>
    {
        private static readonly Task<int> done = Task.FromResult(0);

        private readonly IDateTimeProvider dateTimeProvider;

        private readonly string streamName;
        private bool isInDisposedState = false;

        private readonly IStoreEventsToStream eventWriter;
        private static readonly IValidateState<TState> defaultValidator = new AlwaysPassValidator<TState>();

        private bool streamExists;

        public EventStoreSession(IValidateState<TState> stateValidator,
            IHoldAllConfiguration configuration,
            StreamReadResults readResult,
            IStoreEventsToStream eventWriter,
            string streamName,
            IDateTimeProvider dateTimeProvider = null)
            : base(stateValidator, configuration)
        {
            this.eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
            this.streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();

            streamExists = readResult.StreamExists;
        }

        public Task LoadFromReadResult(StreamReadResults readResults)
            => LoadFromEvents(readResults.Events.Select(x=> new BullOak.Repositories.Appliers.StoredEvent(x.EventType, x.DeserializedEvent, x.PositionInStream)));

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
            if (disposing)
            {
                ConsiderSessionDisposed();
            }

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
        protected override async Task<int> SaveChanges(
            ItemWithType[] eventsToAdd,
            TState snapshot,
            CancellationToken? cancellationToken)
        {
            checked
            {
                CheckDisposedState();
                int nextExpectedRevision;

                if (!streamExists)
                {
                    nextExpectedRevision = await eventWriter.Add
                    (
                        streamName,
                        eventsToAdd,
                        dateTimeProvider,
                        cancellationToken ?? default
                    );
                }
                else
                {
                    nextExpectedRevision = await eventWriter.AppendTo
                    (
                        streamName,
                        ConcurrencyId,
                        eventsToAdd,
                        dateTimeProvider,
                        cancellationToken ?? default
                    );
                }

                ConsiderSessionDisposed();
                return nextExpectedRevision;
            }
        }
    }
}
