[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using BoDi;
    using Contexts;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using TechTalk.SpecFlow;

    [Binding]
    internal sealed class TestsSetupAndTeardown
    {
        private readonly IObjectContainer objectContainer;
        private readonly EventStoreIntegrationContext context;

        public TestsSetupAndTeardown(IObjectContainer objectContainer, EventStoreIntegrationContext context)
        {
            this.objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            this.context = context;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            objectContainer.RegisterInstanceAs<IList<TestDataContext>>(new List<TestDataContext>());
        }

        [BeforeTestRun]
        public static void SetupEventStoreNode()
        {
            EventStoreIntegrationContext.SetupNode();
        }

        [AfterTestRun]
        public static void TeardownNode()
        {
            EventStoreIntegrationContext.TeardownNode();
        }

        [Given(@"the (tcp|grpc) protocol is being used")]
        public Task GivenProtocolIsBeingUsed(string protocol)
        {
            var chosenProtocol = Enum.Parse<Protocol>(ToCamelCase(protocol));
            return context.BuildRepositories(chosenProtocol);
        }

        private static string ToCamelCase(string text)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
