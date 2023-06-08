using BullOak.Repositories.Appliers;

namespace Benchmark.Model;

public class TestApplier:
    IApplyEvent<IHoldTestState, EntityCreated>,
    IApplyEvent<IHoldTestState, EntityUpdated>
{
    public IHoldTestState Apply(IHoldTestState state, EntityCreated @event)
    {
        state.Id = @event.Id;
        state.Name = @event.Name;
        return state;
    }

    public IHoldTestState Apply(IHoldTestState state, EntityUpdated @event)
    {
        state.Id = @event.Id;
        state.Elements = @event.Elements;
        return state;
    }
}
