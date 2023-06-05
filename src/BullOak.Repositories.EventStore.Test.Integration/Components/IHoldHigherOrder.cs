namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    public interface IHoldHigherOrder
    {
        int HigherOrder { get; set; }
        int LastValue { get; set; }

        bool Visility { get; set; }
    }
}
