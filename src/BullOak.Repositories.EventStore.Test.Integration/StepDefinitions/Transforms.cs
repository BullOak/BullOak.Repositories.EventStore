namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using TechTalk.SpecFlow;

    [Binding]
    public class Transforms
    {
        [StepArgumentTransformation]
        public DateTime AsDateTime(string input)
        {
            return DateTime.Parse(input);
        }
    }
}
