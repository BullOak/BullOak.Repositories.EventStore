namespace Benchmark.Model;

public interface IHoldTestState
{
    Guid Id { get; set; }
    string Name { get; set; }
    string[] Elements { get; set; }
}
