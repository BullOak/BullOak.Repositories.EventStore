namespace BullOak.Repositories.EventStore.Test.Integration.Components
{
    public interface IHoldHigherOrder
    {
        int HigherOrder { get; set; }

        bool SoftDeleteFound { get; set; }
    }
}
