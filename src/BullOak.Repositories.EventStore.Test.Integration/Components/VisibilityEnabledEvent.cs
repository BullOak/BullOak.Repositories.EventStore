namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    using System;

    public class VisibilityEnabledEvent : IMyEvent
    {
        public VisibilityEnabledEvent(Guid id, int value)
        {
            Id = id;
            Value = value;
        }
        public Guid Id { get; set; }

        public int Value { get; set; }
    }
}