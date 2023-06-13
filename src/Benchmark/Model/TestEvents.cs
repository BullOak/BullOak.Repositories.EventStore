namespace Benchmark.Model;

public interface ITestEvent { }
public record EntityCreated(Guid Id, string Name) : ITestEvent;
public record EntityUpdated(Guid Id, string[] Elements) : ITestEvent;
