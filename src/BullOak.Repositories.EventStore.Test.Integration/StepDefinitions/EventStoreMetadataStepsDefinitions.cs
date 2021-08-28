namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using TechTalk.SpecFlow;
    using Components;
    using Contexts;

    [Binding]
    [Scope(Feature = "EventStore Metadata Support")]
    internal class EventStoreMetadataStepsDefinitions
    {
        private readonly EventStoreIntegrationContext eventStoreContainer;
        private readonly EventGenerator eventGenerator;
        private string streamId;
        private string streamEventName;
        private bool isNewState;

        public EventStoreMetadataStepsDefinitions(
            EventStoreIntegrationContext eventStoreContainer,
            EventGenerator eventGenerator)
        {
            this.eventStoreContainer = eventStoreContainer ?? throw new ArgumentNullException(nameof(eventStoreContainer));
            this.eventGenerator = eventGenerator ?? throw new ArgumentNullException(nameof(eventGenerator));
            ResetStreamId();
        }

        [Given(@"a stream with events")]
        public Task GivenAStreamWithEvents()
        {
            ResetStreamId();
            var newEvents = eventGenerator.GenerateEvents(5);
            streamEventName = newEvents.First().GetType().Name;
            return eventStoreContainer.WriteEventsToStreamRaw(
                streamId,
                newEvents);
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

        [When(@"I try to open the stream")]
        public async Task WhenITryToOpenTheStream()
        {
            using var session = await eventStoreContainer.StartSession(streamId);
            isNewState = session.IsNewState;
        }

        [When(@"I try to open a projection that uses undefined events")]
        public async Task WhenITryToOpenProjectionForUndefinedEvents()
        {
            var undefinedEventName = Guid.NewGuid().ToString("N");
            using var session = await eventStoreContainer.StartSession($"$et-{undefinedEventName}");
            isNewState = session.IsNewState;
        }

        [When(@"I try to open a projection that uses events from the truncated stream")]
        public async Task WhenITryToOpenProjectionForTruncatedEvents()
        {
            using var session = await eventStoreContainer.StartSession($"$et-{streamEventName}");
            isNewState = session.IsNewState;
        }

        [Then(@"the session reports new state")]
        public void ThenTheSessionReportsNewState()
        {
            isNewState.Should().BeTrue();
        }

        private void ResetStreamId()
        {
            var id = Guid.NewGuid().ToString("N");
            streamId = $"metadata_test_{id}";
        }
    }
}
