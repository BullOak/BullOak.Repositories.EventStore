namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Contexts;
    using TechTalk.SpecFlow;
    using Xunit;

    [Binding]
    internal class ModelLoadingStepDefinitions
    {
        private readonly EventStoreIntegrationContext eventStoreContainer;
        private readonly IList<TestDataContext> testDataContexts;

        public ModelLoadingStepDefinitions(EventStoreIntegrationContext eventStoreContainer, IList<TestDataContext> testDataContexts)
        {
            this.eventStoreContainer =
                eventStoreContainer ?? throw new ArgumentNullException(nameof(eventStoreContainer));
            this.testDataContexts =
                testDataContexts ?? throw new ArgumentNullException(nameof(testDataContexts));
        }

        [When("I load my entity ignoring any errors")]
        public async Task WhenILoadIgnoringAnyPreviousErrors()
        {
            var testDataContext = testDataContexts.First();
            using (var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId))
            {
                testDataContext.LatestLoadedState = session.GetCurrentState();
            }
        }

        [When(@"I load my entity")]
        public async Task WhenILoadMyEntity()
        {
            var testDataContext = testDataContexts.First();
            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
                {
                    await WhenILoadIgnoringAnyPreviousErrors();
                });
        }

        [When(@"I load my entity as of '(.*)'")]
        public async Task WhenILoadMyEntityAsOf(string applyAtTimeStr)
        {
            var applyAtTime = DateTime.Parse(applyAtTimeStr);
            var testDataContext = testDataContexts.First();

            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                using (var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId, applyAtTime))
                {
                    testDataContext.LatestLoadedState = session.GetCurrentState();
                }
            });
        }


        [When(@"I load my entity through the read-only repository")]
        public async Task WhenILoadMyEntityThroughTheRead_OnlyRepository()
        {
            var testDataContext = testDataContexts.First();

            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                var readModel = await eventStoreContainer.readOnlyRepository.ReadFrom(testDataContext.CurrentStreamId.ToString());
                testDataContext.LatestLoadedState = readModel.state;
                testDataContext.LastConcurrencyId = readModel.concurrencyId;
            });
        }

        [When(@"I load my entity through the read-only repository as of '(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})'")]
        public async Task WhenILoadMyEntityThroughTheRead_OnlyRepositoryAsOf(DateTime appliesAt)
        {
            var testDataContext = testDataContexts.First();

            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                var state = await eventStoreContainer.readOnlyRepository.ReadFrom(testDataContext.CurrentStreamId.ToString(), appliesAt);
                testDataContext.LatestLoadedState = state;
            });
        }
    }
}
