namespace BullOak.Repositories.EventStore
{
    using BullOak.Repositories.Session;

    public class AlwaysPassValidator<TState> : IValidateState<TState>
    {
        /// <inheritdoc />
        public ValidationResults Validate(TState state)
            => ValidationResults.Success();
    }
}