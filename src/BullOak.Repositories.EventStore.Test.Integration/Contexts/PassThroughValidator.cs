namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using BullOak.Repositories.EventStore.Test.Integration.Components;
    using BullOak.Repositories.Session;

    internal class PassThroughValidator : IValidateState<IHoldHigherOrder>
    {
        public IValidateState<IHoldHigherOrder> CurrentValidator { get; set; }

        public PassThroughValidator() => CurrentValidator = new AlwaysPassValidator<IHoldHigherOrder>();

        /// <inheritdoc />
        public ValidationResults Validate(IHoldHigherOrder state) => CurrentValidator.Validate(state);
    }
}
