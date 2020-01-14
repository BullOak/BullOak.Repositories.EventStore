namespace BullOak.Repositories.EventStore.Test.Integration.Contexts.EventStoreIsolation
{
    using System;

    public static class IsolationFactory
    {
        public static IDisposable StartIsolation(
            IsolationMode isolationMode,
            string isolationCommand,
            string isolationArguments)
        {
            switch (isolationMode)
            {
                case IsolationMode.None: return new NoopIsolation();
                case IsolationMode.Process: return new ProcessIsolation(isolationCommand, isolationArguments);
                default:
                    throw new InvalidOperationException($"Isolation mode {isolationMode} is not supported.");
            }
        }
    }
}
