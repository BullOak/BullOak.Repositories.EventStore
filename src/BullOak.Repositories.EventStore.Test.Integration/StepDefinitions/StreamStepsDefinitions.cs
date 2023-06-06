namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using Components;
    using Contexts;
    using Exceptions;
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Events;
    using Polly;
    using Polly.Retry;
    using TechTalk.SpecFlow;
    using Xunit;

    [Binding]
    internal class StreamStepsDefinitions
    {
        private readonly EventStoreIntegrationContext eventStoreContainer;
        private readonly EventGenerator eventGenerator;
        private readonly IList<TestDataContext> testDataContexts;
        private List<ReadModel<IHoldHigherOrder>> States;
        private readonly string sharedCategoryName = RandomString(15);
        private static readonly Random random = new Random();
        private readonly AsyncRetryPolicy retry = Policy.Handle<ReadModelConsistencyException>().WaitAndRetryAsync(25, count => TimeSpan.FromMilliseconds(200));

        public StreamStepsDefinitions(
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

        [Given(@"I update the state of visible to be enabled as of '(.*)'")]
        public async Task GivenIUpdateTheStateOfVisibleToBeEnabled(DateTime datetime)
        {
            var testDataContext = testDataContexts.First();
            var visibleEvent = new VisibilityEnabledEvent(testDataContext.RawStreamId, 0);
            eventStoreContainer.DateTimeProvider.AddTestTimes(new List<DateTime>{ datetime });

            using (var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId))
            {
                testDataContext.LastGeneratedEvents = new List<IMyEvent>{ visibleEvent };
                session.AddEvent(visibleEvent);

                await session.SaveChanges();
            }
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
                    using var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId);
                    foreach (var @event in testDataContext.LastGeneratedEvents)
                    {
                        session.AddEvent<IMyEvent>(m =>
                        {
                            m.Id = @event.Id;
                            m.Value = @event.Value;
                        });
                    }

                    await session.SaveChanges();
                });
            }
        }

        [When(@"I load all my entities as of '(.*)' for the streams category")]
        public async Task WhenILoadAllMyEntitiesAsOf(DateTime dateTime)
            => await LoadAllEntities(dateTime);

        [When(@"I load all my entities for the streams category")]
        public async Task WhenILoadAllMyEntities()
            => await LoadAllEntities(null);

        [Given(@"I try to save the new events in the stream")]
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

        [Then(@"the load process should succeed")]
        [Then(@"the save process should succeed")]
        public void ThenTheSaveProcessShouldSucceed()
        {
            var exceptionObserved = false;

            foreach (var testDataContext in testDataContexts)
            {
                if (testDataContext.RecordedException == null)
                    continue;

                Console.WriteLine(testDataContext.RecordedException);
                exceptionObserved = true;
            }

            exceptionObserved.Should().BeFalse();
        }

        [Then(@"HighOrder property for stream (.*) should be (.*)")]
        public void ThenHighOrderPropertyForStreamShouldBe(int streamNumber, int highestOrderValue)
        {
            States.ElementAt(--streamNumber).state.HigherOrder.Should().Be(highestOrderValue);
        }

        [Then(@"the visibilty should be disabled")]
        public void ThenTheVisibiltyShouldBeEnabled()
        {
            testDataContexts.First().LatestLoadedState.Visility.Should().BeFalse();
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

        [Then(@"LastState property should be (.*)")]
        public void ThenLastStatePropertyShouldBe(int lastValue)
        {
            testDataContexts.First().LatestLoadedState.LastValue.Should().Be(lastValue);
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
            testDataContext.ResetStream(sharedCategoryName);

            testDataContexts.Add(testDataContext);
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task LoadAllEntities(DateTime? dateTime)
        {
            if (testDataContexts.Any(x => x.RecordedException != null)) return;

            var testDataContext = testDataContexts.First();

            var categoryName = testDataContext.StreamIdPrefix;

            await retry.ExecuteAsync(async () =>
            {
                var readModels = (await eventStoreContainer.ReadAllEntitiesFromCategory(categoryName, dateTime)).ToList();

                if (readModels.Count < testDataContexts.Count)
                    throw new ReadModelConsistencyException(
                        $"Expected to read {testDataContexts.Count} read models but found {readModels.Count} read models in the DB");

                if (testDataContexts.Count == 1)
                    testDataContexts.First().LatestLoadedState = readModels.First().state;

                States = readModels;
            });
        }

        [Given(@"I try to save the new events in the stream through IStoreEventsToStream interface")]
        public async Task GivenITryToSaveTheNewEventsInTheStreamThroughIStoreEventsToStreamInterface()
        {
            foreach (var testDataContext in testDataContexts)
            {
                testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
                {
                    var events = testDataContext.LastGeneratedEvents
                        .Select(evt => new ItemWithType(evt))
                        .ToArray();
                    await eventStoreContainer.EventWriter.Add(
                        testDataContext.CurrentStreamId, events, eventStoreContainer.DateTimeProvider);
                });
            }
        }

        [Then(@"events returned should be (.*)")]
        public async Task ThenEventsReturnedShouldBe(int expected)
        {
            var context = testDataContexts.First();
            var actual = await context.LatestStreamReadResults.Events.ToListAsync();
            actual.Should().HaveCount(expected);
        }

        [Then(@"stream position should be (.*)")]
        public void ThenStreamPositionShouldBe(int expected)
        {
            var actual = testDataContexts.First().LatestStreamReadResults.StoredEventPosition;
            actual.ToInt64().Should().Be(expected);
        }

        [When(@"I try to open the new stream")]
        public async Task WhenITryToOpenANewStream()
        {
            using (var session = await eventStoreContainer.StartSession(testDataContexts.First().CurrentStreamId))
            {
                testDataContexts.First().IsNewState = session.IsNewState;
                testDataContexts.First().LatestLoadedState = session.GetCurrentState();
            }
        }

        [Then(@"the session reports new state")]
        public void ThenTheSessionReportsNewState()
        {
            testDataContexts.First().IsNewState.Should().BeTrue();
        }
    }
}
