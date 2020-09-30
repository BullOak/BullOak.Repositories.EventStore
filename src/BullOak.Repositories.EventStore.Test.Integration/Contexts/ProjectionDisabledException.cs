namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using System;

    internal class ProjectionDisabledException : Exception
    {
        public ProjectionDisabledException(string message) : base(message)
        {
        }
    }
}