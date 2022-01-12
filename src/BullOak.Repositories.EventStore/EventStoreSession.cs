using BullOak.Repositories.EventStore.Events;
using BullOak.Repositories.Exceptions;
using EventStore.Client;

namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.Session;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Streams;

    public class EventStoreSession<TState> : BaseEventSourcedSession<TState, long>
    {
        private static readonly Task<int> done = Task.FromResult(0);

        private readonly IDateTimeProvider dateTimeProvider;

        private readonly string streamName;
        private bool isInDisposedState = false;

        private readonly IReadEventsFromStream eventReader;
        private readonly IStoreEventsToStream eventWriter;
        private static readonly IValidateState<TState> defaultValidator = new AlwaysPassValidator<TState>();

        private bool streamExists;

        public EventStoreSession
        (
            IHoldAllConfiguration configuration,
            IReadEventsFromStream eventReader,
            IStoreEventsToStream eventWriter,
            string streamName,
            IDateTimeProvider dateTimeProvider = null
        ) : this(defaultValidator, configuration, eventReader, eventWriter, streamName, dateTimeProvider)
        {
        }

        public EventStoreSession
        (
            IValidateState<TState> stateValidator,
            IHoldAllConfiguration configuration,
            IReadEventsFromStream eventReader,
            IStoreEventsToStream eventWriter,
            string streamName,
            IDateTimeProvider dateTimeProvider = null
        ) : base(stateValidator, configuration)
        {
            this.eventReader = eventReader ?? throw new ArgumentNullException(nameof(eventReader));
            this.eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));

            this.streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
        }

        public async Task Initialize(Func<IAmAStoredEvent, bool> loadEventPredicate = null)
        {
            CheckDisposedState();
            //TODO: user credentials
            var data = await eventReader.ReadFrom(streamName, loadEventPredicate);

            streamExists = data.StreamExists;

            var events = await data.Events.Reverse().ToArrayAsync();

            LoadFromEvents(events.Select(x => x.ToItemWithType()).ToArray(), data.StoredEventPosition.ToInt64());
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
        protected override async Task<int> SaveChanges(ItemWithType[] eventsToAdd,
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
