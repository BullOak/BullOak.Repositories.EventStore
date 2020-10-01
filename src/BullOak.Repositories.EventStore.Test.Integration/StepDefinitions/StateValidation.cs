namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using FluentAssertions;
    using TechTalk.SpecFlow;

    [Binding]
    internal class StateValidation
    {
        private readonly IList<TestDataContext> testDataContext;

        public StateValidation(IList<TestDataContext> testDataContext)
        {
            this.testDataContext = testDataContext ?? throw new ArgumentNullException(nameof(testDataContext));
        }

        [Then(@"have a concurrency id of (.*)")]
        public void ThenHaveAConcurrencyIdOf(int expectedConcurrencyId)
        {
            testDataContext.First().LastConcurrencyId.Should().Be(expectedConcurrencyId);
        }
    }
}
