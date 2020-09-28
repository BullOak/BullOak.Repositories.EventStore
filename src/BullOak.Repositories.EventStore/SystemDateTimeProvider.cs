namespace BullOak.Repositories.EventStore
{
    using System;

    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
