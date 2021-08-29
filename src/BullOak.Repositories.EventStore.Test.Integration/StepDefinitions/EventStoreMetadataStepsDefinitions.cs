namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using TechTalk.SpecFlow;
    using Components;
    using Contexts;
    using Session;

    [Binding]
    [Scope(Feature = "EventStore Metadata Support")]
    internal class EventStoreMetadataStepsDefinitions
    {
        private readonly EventStoreIntegrationContext eventStoreContainer;
        private readonly EventGenerator eventGenerator;
        private string streamId;
        private string streamEventName;
        private bool isNewState;
        private IHoldHigherOrder state;

        public EventStoreMetadataStepsDefinitions(
            EventStoreIntegrationContext eventStoreContainer,
            EventGenerator eventGenerator)
        {
            this.eventStoreContainer = eventStoreContainer ?? throw new ArgumentNullException(nameof(eventStoreContainer));
            this.eventGenerator = eventGenerator ?? throw new ArgumentNullException(nameof(eventGenerator));

            ResetStreamId();
            isNewState = false;
            state = default;
        }

        [Given(@"a stream with events")]
        public Task GivenAStreamWithEvents()
        {
            ResetStreamId();
            return GivenIAddSomeEventsToTheStream();
        }

        [Given(@"I delete the stream in EventStore")]
        public Task GivenIDeleteStreamInEventStore()
        {
            return eventStoreContainer.SoftDeleteStream(streamId);
        }

        [Given(@"I truncate the stream in EventStore")]
        public async Task GivenITruncateStreamInEventStore()
        {
            await eventStoreContainer.TruncateStream(streamId);
            await eventStoreContainer.AssertStreamHasNoResolvedEvents(streamId);
        }

        [Given(@"I add some new events to the stream")]
        public Task GivenIAddSomeEventsToTheStream()
        {
            var newEvents = eventGenerator.GenerateEvents(5);
            streamEventName = newEvents.First().GetType().AssemblyQualifiedName;
            return eventStoreContainer.WriteEventsToStreamRaw(
                streamId,
                newEvents);
        }

        [When(@"I try to open the stream")]
        public async Task WhenITryToOpenTheStream()
        {
            using var session = await eventStoreContainer.StartSession(streamId);
            RecordCurrentState(session);
        }

        [When(@"I try to open a projection that uses undefined events")]
        public async Task WhenITryToOpenProjectionForUndefinedEvents()
        {
            var undefinedEventName = Guid.NewGuid().ToString("N");
            using var session = await eventStoreContainer.StartSession($"$et-{undefinedEventName}");
            RecordCurrentState(session);
        }

        [When(@"I try to open a projection that uses events from the truncated stream")]
        public async Task WhenITryToOpenProjectionForTruncatedEvents()
        {
            var projectionName = $"$et-{streamEventName}";
            await eventStoreContainer.AssertStreamHasSomeUnresolvedEvents(projectionName);

            using var session = await eventStoreContainer.StartSession(projectionName);
            RecordCurrentState(session);
        }

        [Then(@"the session reports new state")]
        public void ThenTheSessionReportsNewState()
        {
            isNewState.Should().BeTrue();
        }

        [Then(@"the session can rehydrate the state")]
        public void ThenTheSessionCanRehydrateState()
        {
            state.Should().NotBeNull();
        }

        private void ResetStreamId()
        {
            var id = Guid.NewGuid().ToString("N");
            streamId = $"metadata_test_{id}";
        }

        private void RecordCurrentState(IManageSessionOf<IHoldHigherOrder> session)
        {
            isNewState = session.IsNewState;
            state = isNewState ? default : session.GetCurrentState();
        }
    }
}
