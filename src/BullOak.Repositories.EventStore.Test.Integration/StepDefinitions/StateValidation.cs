namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using BullOak.Repositories.EventStore.Test.Integration.Contexts;
    using FluentAssertions;
    using TechTalk.SpecFlow;

    [Binding]
    internal class StateValidation
    {
        private readonly TestDataContext testDataContext;

        public StateValidation(TestDataContext testDataContext)
        {
            this.testDataContext = testDataContext ?? throw new ArgumentNullException(nameof(testDataContext));
        }

        [Then(@"have a concurrency id of (.*)")]
        public void ThenHaveAConcurrencyIdOf(int expectedConcurrencyId)
        {
            testDataContext.LastConcurrencyId.Should().Be(expectedConcurrencyId);
        }
    }
}
