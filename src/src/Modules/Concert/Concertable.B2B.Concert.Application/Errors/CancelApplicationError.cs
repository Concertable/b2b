using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelApplicationError : IError
{
    public ErrorDefinition Definition => this switch
    {
        TransitionFailure(var error) => error.Definition,
        InvalidState(var state) => ErrorDefinition.Conflict<InvalidState>(
            $"Cannot cancel an application from {state}."),
        EscrowRefundFailure(var error) => error.Definition
    };

    public partial record TransitionFailure(LifecycleTransitionError Error);

    [ErrorCode("application.cancel.invalid_state")]
    public partial record InvalidState(LifecycleState State);

    public partial record EscrowRefundFailure(EscrowRefundError Error);
}
