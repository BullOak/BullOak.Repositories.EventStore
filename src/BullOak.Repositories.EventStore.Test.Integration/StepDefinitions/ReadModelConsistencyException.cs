namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;

    internal class ReadModelConsistencyException : Exception
    {
        public ReadModelConsistencyException(string message) : base(message)
        {
        }
    }
}
