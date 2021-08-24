using System.Threading.Tasks;

namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using TechTalk.SpecFlow;

    [Binding]
    internal class WaitStepDefinition
    {
        [Then("after waiting for ([0-9]*) seconds for categories to be processed")]
        public Task WaitForAFewSeconds(int secondsToWait)
            => Task.Delay(secondsToWait * 1000);

    }
}
