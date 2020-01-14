namespace BullOak.Repositories.EventStore
{
    public struct ReadModel<TState>
    {
        public readonly TState state;
        public readonly int concurrencyId;

        public ReadModel(TState state, int concurrencyId)
        {
            this.state = state;
            this.concurrencyId = concurrencyId;
        }

        public static implicit operator TState(ReadModel<TState> readModel)
            => readModel.state;
    }
}
