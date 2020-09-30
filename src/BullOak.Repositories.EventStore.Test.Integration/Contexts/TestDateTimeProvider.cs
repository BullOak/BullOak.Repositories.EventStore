namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TestDateTimeProvider : IDateTimeProvider
    {
        private readonly Queue<DateTime> times = new Queue<DateTime>();

        public void AddTestTimes(IEnumerable<DateTime> times)
        {
            foreach (var time in times)
                this.times.Enqueue(time);
        }

        public DateTime UtcNow => times.Any() ? times.Dequeue() : DateTime.UtcNow.AddHours(-5);
    }
}
