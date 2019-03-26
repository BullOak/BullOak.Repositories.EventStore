namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using System.Threading.Tasks;
    using BullOak.Repositories.EventStore.Test.Integration.Components;
    using BullOak.Repositories.EventStore.Test.Integration.Contexts;
    using TechTalk.SpecFlow;
    using Xunit;

    [Binding]
    internal class ModelLoadingStepDefinitions
    {
        private readonly InProcEventStoreIntegrationContext eventStoreContainer;
        private readonly TestDataContext testDataContext;

        public ModelLoadingStepDefinitions(InProcEventStoreIntegrationContext eventStoreContainer,
            TestDataContext testDataContext)
        {
            this.eventStoreContainer =
                eventStoreContainer ?? throw new ArgumentNullException(nameof(eventStoreContainer));
            this.testDataContext = testDataContext ?? throw new ArgumentNullException(nameof(testDataContext));
        }

        [When(@"I load my entity")]
        public async Task WhenILoadMyEntity()
        {
            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                using (var session = await eventStoreContainer.StartSession(testDataContext.CurrentStreamId))
                {
                    testDataContext.LatestLoadedState = session.GetCurrentState();
                }
            });
        }

        [When(@"I load my entity through the read-only repository")]
        public async Task WhenILoadMyEntityThroughTheRead_OnlyRepository()
        {
            if (testDataContext.RecordedException != null) return;

            testDataContext.RecordedException = await Record.ExceptionAsync(async () =>
            {
                var readModel = await eventStoreContainer.readOnlyRepository.ReadFrom(testDataContext.CurrentStreamId.ToString());
                testDataContext.LatestLoadedState = readModel.state;
                testDataContext.LastConcurrencyId = readModel.concurrencyId;
            });
        }
    }
}
