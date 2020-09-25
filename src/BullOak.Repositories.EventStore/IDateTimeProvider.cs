namespace BullOak.Repositories.EventStore
{
    using System;

    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
