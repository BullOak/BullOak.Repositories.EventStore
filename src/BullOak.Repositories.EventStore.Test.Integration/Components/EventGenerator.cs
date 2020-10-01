namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class EventGenerator
    {
        public List<MyEvent> GenerateEvents(int count)
            => Enumerable.Range(0, count).Select(x => new MyEvent(x)).ToList();

        public List<MyEvent> GenerateEvents(Guid id, int count)
            => Enumerable.Range(0, count).Select(x =>
            {
                var myEvent = new MyEvent(x) { Id = id };
                return myEvent;
            }).ToList();
    }
}
