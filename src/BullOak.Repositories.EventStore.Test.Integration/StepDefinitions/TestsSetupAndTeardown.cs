namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using BoDi;
    using Contexts;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TechTalk.SpecFlow;

    [Binding]
    public sealed class TestsSetupAndTeardown
    {
        private readonly IObjectContainer objectContainer;

        public TestsSetupAndTeardown(IObjectContainer objectContainer)
        {
            this.objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            objectContainer.RegisterInstanceAs<IList<TestDataContext>>(new List<TestDataContext>());
        }

        [BeforeTestRun]
        public static async Task SetupEventStoreNode()
        {
            await EventStoreIntegrationContext.SetupNode();
        }

        [AfterTestRun]
        public static void TeardownNode()
        {
            EventStoreIntegrationContext.TeardownNode();
        }
    }
}
