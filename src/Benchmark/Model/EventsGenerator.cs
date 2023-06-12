namespace Benchmark.Model;

public static class EventsGenerator
{
    public static string GetStreamId(int eventsCount, int eventSize) => $"{eventsCount:000000}_{eventSize:0000}";

    public static (string StreamId, IReadOnlyList<ITestEvent> Events) Generate(int eventsCount, int eventElements)
    {
        var entityId = Guid.NewGuid();

        return
            (GetStreamId(eventsCount, eventElements),
                new ITestEvent[] { new EntityCreated(entityId, Guid.NewGuid().ToString("N")) }
                    .Concat(Enumerable.Range(0, eventsCount)
                        .Select(_ =>
                            new EntityUpdated(
                                entityId,
                                Enumerable.Range(0, eventElements)
                                    .Select(_ => Guid.NewGuid().ToString("N"))
                                    .ToArray())))
                    .ToList());
    }
}
