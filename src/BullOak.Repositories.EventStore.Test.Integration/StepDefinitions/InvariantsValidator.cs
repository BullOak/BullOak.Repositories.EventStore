namespace BullOak.Repositories.EventStore.Test.Integration.StepDefinitions
{
    using System;
    using BullOak.Repositories.EventStore.Test.Integration.Components;
    using BullOak.Repositories.EventStore.Test.Integration.Contexts;
    using BullOak.Repositories.Session;
    using TechTalk.SpecFlow;

    [Binding]
    internal class InvariantsValidator
    {
        public PassThroughValidator validator;

        public InvariantsValidator(PassThroughValidator validator)
            => this.validator = validator ?? throw new ArgumentNullException(nameof(validator));

        [Given(@"an always-fail invariant checker")]
        public void GivenAnAlways_FailInvariantChecker()
        {
            validator.CurrentValidator = new AlwaysFailValidator();
        }

        [Given(@"an invariant checker that allows a maximum higher order of (.*)")]
        public void GivenAnAlways_FailInvariantChecker(int maxOrderAccepted)
        {
            validator.CurrentValidator = new FailIfHighOrderIsMoreThanValidator(maxOrderAccepted);
        }

        private class AlwaysFailValidator : IValidateState<IHoldHigherOrder>
        {
            /// <inheritdoc />
            public ValidationResults Validate(IHoldHigherOrder state)
                => ValidationResults.Errors(new BasicValidationError[] {"Always fail"});
        }

    }

    internal class FailIfHighOrderIsMoreThanValidator : IValidateState<IHoldHigherOrder>
    {
        private readonly int maxOrderAccepted;

        public FailIfHighOrderIsMoreThanValidator(int maxOrderAccepted)
            => this.maxOrderAccepted = maxOrderAccepted;


        /// <inheritdoc />
        public ValidationResults Validate(IHoldHigherOrder state)
            => state.HigherOrder <= maxOrderAccepted
                ? ValidationResults.Success()
                : ValidationResults.Errors(new BasicValidationError[] {"Higher order is too high!!"});
    }
}
