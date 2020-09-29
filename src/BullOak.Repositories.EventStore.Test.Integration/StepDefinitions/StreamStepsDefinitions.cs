﻿namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using Components;
    using Contexts;
    using Exceptions;
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TechTalk.SpecFlow;
    using Xunit;

    [Binding]
    internal class SaveEventsStreamSteps
    {
        private readonly EventStoreIntegrationContext eventStoreContainer;
        private readonly EventGenerator eventGenerator;
        private readonly IList<TestDataContext> testDataContexts;

        public SaveEventsStreamSteps(
            EventStoreIntegrationContext eventStoreContainer,
            EventGenerator eventGenerator,
            IList<TestDataContext> testDataContexts)
        {
            this.eventStoreContainer = eventStoreContainer ?? throw new ArgumentNullException(nameof(eventStoreContainer));
            this.eventGenerator = eventGenerator ?? throw new ArgumentNullException(nameof(eventGenerator));
            this.testDataContexts = testDataContexts ?? throw new ArgumentNullException(nameof(testDataContexts));
        }

        [Given(@"a new stream")]
        [Given(@"another new stream")]
        public void GivenANewStream() => AddStream();

        [Given(@"an existing stream with (.*) events")]
        public Task GivenAnExistingStreamWithEvents(int count)
        {
            AddStream();
            return eventStoreContainer.WriteEventsToStreamRaw(
                testDataContexts.First().CurrentStreamId,
                eventGenerator.GenerateEvents(count));
        }

        [Given(@"(.*) new events?")]
        public void GivenNewEvents(int eventsNumber)
        {
            var events = eventGenerator.GenerateEvents(eventsNumber);
            testDataContexts.First().LastGeneratedEvents.Clear();
            testDataContexts.First().LastGeneratedEvents.AddRange(events);
        }

        [Given(@"the following events with the following timestamps")]
        public void GivenNewEventsWithTheFollowingTimestamps(Table table)
        {
            var events = eventGenerator.GenerateEvents(table.RowCount);

            var times = table.Rows.Select(x => DateTime.Parse(x["Timestamp"]));
            eventStoreContainer.DateTimeProvider.AddTestTimes(times);

            testDataContexts.First().LastGeneratedEvents.AddRange(events);
        }

        [Given(@"the following events with timestamps for stream (.*)")]
        public void GivenTheFollowingEventsWithTimestampsForStream(int streamNumber, Table table)
        {
            var testDataContext = testDataContexts.ElementAt(--streamNumber);
            var streamId = testDataContext.RawStreamId;
            var events = eventGenerator.GenerateEvents(streamId, table.RowCount);

            var times = table.Rows.Select(x => DateTime.Parse(x["Timestamp"]));
            eventStoreContainer.DateTimeProvider.AddTestTimes(times);

            testDataContext.LastGeneratedEvents.AddRange(events);
        }

        [Given(@"I try to save the new events in the stream through their interface")]
        [When(@"I try to save the new events in the stream through their interface")]
        public async Task GivenITryToSaveTheNewEventsInTheStreamThroughTheirInterface()
        {
            foreach (var testDataContext in testDataContexts)
            {
                testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
                {
                    using (var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId))
                    {
                        foreach (var @event in testDataContext.LastGeneratedEvents)
                        {
                            session.AddEvent<IMyEvent>(m =>
                            {
                                m.Id = @event.Id;
                                m.Value = @event.Value;
                            });
                        }

                        await session.SaveChanges();
                    }
                });
            }
        }

        [When(@"I load all my entities as of '(.*)' for the streams category")]
        public async Task WhenILoadAllMyEntitiesAsOf(DateTime dateTime)
        {
            var testDataContext = testDataContexts.First();

            if (testDataContext.RecordedException != null) return;

            var categoryName = testDataContext.StreamIdPrefix;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                using (var session = await eventStoreContainer.StartSessionForCategory(categoryName, dateTime))
                {

                    var thing = session.GetCurrentState();
                }
            });
        }


        [When(@"I try to save the new events in the stream")]
        public async Task WhenITryToSaveTheNewEventsInTheStream()
        {
            foreach (var testDataContext in testDataContexts)
            {
                testDataContext.RecordedException = await Record.ExceptionAsync(() =>
                    eventStoreContainer.AppendEventsToCurrentStream(
                        testDataContext.CurrentStreamId,
                        testDataContext.LastGeneratedEvents.ToArray()));
            }
        }

        [Given(@"I soft-delete the stream")]
        public Task GivenISoft_DeleteTheStream()
            => eventStoreContainer.SoftDeleteStream(testDataContexts.First().CurrentStreamId);

        [Given(@"I hard-delete the stream")]
        public Task GivenIHard_DeleteTheStream()
            => eventStoreContainer.HardDeleteStream(testDataContexts.First().CurrentStreamId);

        [Given(@"I soft-delete-by-event the stream")]
        [When(@"I soft-delete-by-event the stream")]
        public Task GivenI_Soft_Delete_by_EventTheStream()
            => eventStoreContainer.SoftDeleteByEvent(testDataContexts.First().CurrentStreamId);

        [Given(@"I soft-delete-by-custom-event the stream")]
        [When(@"I soft-delete-by-custom-event the stream")]
        public Task GivenI_Soft_Delete_by_Custom_EventTheStream()
            => eventStoreContainer.SoftDeleteByEvent(testDataContexts.First().CurrentStreamId, () => new MyEntitySoftDeleted());

        [Then(@"the load process should succeed")]
        [Then(@"the save process should succeed")]
        public void ThenTheSaveProcessShouldSucceed()
        {
            testDataContexts.All(x => x.RecordedException == null).Should().BeTrue();
        }

        [Then(@"HighOrder property for stream (.*) should be (.*)")]
        public void ThenHighOrderPropertyForStreamShouldBe(int streamNumber, int highestOrderValue)
        {
            testDataContexts.ElementAt(--streamNumber).LatestLoadedState.HigherOrder.Should().Be(highestOrderValue);
        }

        [Then(@"the save process should fail")]
        public void ThenTheSaveProcessShouldFail()
        {
            testDataContexts.First().RecordedException.Should().NotBeNull();
        }

        [Then(@"there should be (.*) events in the stream")]
        public async Task ThenThereShouldBeEventsInTheStream(int count)
        {
            var recordedEvents = await eventStoreContainer.ReadEventsFromStreamRaw(testDataContexts.First().CurrentStreamId);
            recordedEvents.Length.Should().Be(count);
        }

        [Then(@"HighOrder property should be (.*)")]
        public void ThenHighOrderPropertyShouldBe(int highestOrderValue)
        {
            testDataContexts.First().LatestLoadedState.HigherOrder.Should().Be(highestOrderValue);
        }

        [When(@"I add (.*) events in the session without saving it")]
        public async Task WhenIAddEventsInTheSessionWithoutSavingIt(int eventCount)
        {
            using (var session = await eventStoreContainer.StartSession(testDataContexts.First().CurrentStreamId))
            {
                session.AddEvents(eventGenerator.GenerateEvents(eventCount));

                testDataContexts.First().LatestLoadedState = session.GetCurrentState();
            }
        }

        [Given(@"session '(.*)' is open")]
        [When(@"I open session '(.*)'")]
        public async Task GivenSessionIsOpen(string sessionName)
        {
            var testDataContext = testDataContexts.First();
            testDataContext.NamedSessions.Add(
                sessionName,
                await eventStoreContainer.StartSession(testDataContext.CurrentStreamId));
        }

        [When(@"I try to add (.*) new events to '(.*)'")]
        [Given(@"(.*) new events are added by '(.*)'")]
        public void GivenNewEventsAreAddedBy(int count, string sessionName)
        {
            testDataContexts.First().NamedSessions[sessionName].AddEvents(eventGenerator.GenerateEvents(count));
        }

        [When(@"I try to save '(.*)'")]
        public async Task WhenITryToSave(string sessionName)
        {
            foreach (var testDataContext in testDataContexts)
            {
                if (!testDataContext.NamedSessionsExceptions.ContainsKey(sessionName))
                {
                    testDataContext.NamedSessionsExceptions.Add(sessionName, new List<Exception>());
                }
                var recordedException = await Record.ExceptionAsync(() => testDataContext.NamedSessions[sessionName].SaveChanges());
                if (recordedException != null)
                {
                    testDataContext.NamedSessionsExceptions[sessionName].Add(recordedException);
                }
            }
        }

        [Then(@"the save process should succeed for '(.*)'")]
        public void ThenTheSaveProcessShouldSucceedFor(string sessionName)
        {
            testDataContexts.First().NamedSessionsExceptions[sessionName].Should().BeEmpty();
        }

        [Then(@"the save process should fail for '(.*)' with ConcurrencyException")]
        public void ThenTheSaveProcessShouldFailForWithConcurrencyException(string sessionName)
        {
            var testDataContext = testDataContexts.First();
            testDataContext.NamedSessionsExceptions[sessionName].Should().NotBeEmpty();
            testDataContext.NamedSessionsExceptions[sessionName].Count.Should().Be(1);
            testDataContext.NamedSessionsExceptions[sessionName][0].Should().BeOfType<ConcurrencyException>();
        }

        [Then(@"the save process should fail for '(.*)'")]
        public void ThenTheSaveProcessShouldFailFor(string sessionName)
        {
            testDataContexts.First().NamedSessionsExceptions[sessionName].Should().NotBeEmpty();
        }

        private void AddStream()
        {
            var testDataContext = new TestDataContext();
            testDataContext.ResetStream();

            testDataContexts.Add(testDataContext);
        }
    }
}
