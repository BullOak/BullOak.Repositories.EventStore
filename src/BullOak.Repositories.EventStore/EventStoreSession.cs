﻿namespace BullOak.Repositories.EventStore
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
    using Newtonsoft.Json.Linq;
    using Streams;

    public class EventStoreSession<TState> : BaseEventSourcedSession<TState, int>
    {
        private static readonly Task<int> done = Task.FromResult(0);
        private readonly IEventStoreConnection eventStoreConnection;
        private readonly string streamName;
        private bool isInDisposedState = false;
        private readonly EventReader eventReader;
        private static readonly IValidateState<TState> defaultValidator = new AlwaysPassValidator<TState>();

        public EventStoreSession(IHoldAllConfiguration configuration,
            IEventStoreConnection eventStoreConnection,
            string streamName)
            : this(defaultValidator, configuration, eventStoreConnection, streamName)
        { }

        public EventStoreSession(IValidateState<TState> stateValidator,
            IHoldAllConfiguration configuration,
            IEventStoreConnection eventStoreConnection,
            string streamName)
            : base(stateValidator, configuration)
        {
            this.eventStoreConnection =
                eventStoreConnection ?? throw new ArgumentNullException(nameof(eventStoreConnection));
            this.streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));

            this.eventReader = new EventReader(eventStoreConnection, configuration);
        }

        public async Task Initialize(DateTime? upTo = null)
        {
            CheckDisposedState();
            //TODO: user credentials
            var streamData = await eventReader.ReadFrom(streamName, upTo);
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
                        eventsToAdd.Select(eventObject => eventObject.CreateEventData()))
                    .ConfigureAwait(false);

                StreamAppendHelpers.CheckConditionalWriteResultStatus(writeResult, streamName);

                if (!writeResult.NextExpectedVersion.HasValue)
                {
                    throw new InvalidOperationException("Eventstore data write outcome unexpected. NextExpectedVersion is null");
                }

                await Initialize();
                ConsiderSessionDisposed();
                return (int)writeResult.NextExpectedVersion.Value;
            }
        }
    }
}
