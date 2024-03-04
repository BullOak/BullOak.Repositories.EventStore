using System;

namespace BullOak.Repositories.EventStore.Streams;

public class EventWriterRetrySettings
{
    public int Limit { get; set; }
    public TimeSpan IntervalDelta { get; set; }
    public bool FastFirst { get; set; }

    public static EventWriterRetrySettings DefaultRetrySettings = new ()
    {
        Limit = 5,
        FastFirst = true,
        IntervalDelta = TimeSpan.FromMilliseconds(200)
    };
}
