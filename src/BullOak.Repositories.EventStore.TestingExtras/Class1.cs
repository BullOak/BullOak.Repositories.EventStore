using System;

namespace BullOak.Repositories.EventStore.TestingExtras
{
    public class SuperHiddenType
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
    }
}
