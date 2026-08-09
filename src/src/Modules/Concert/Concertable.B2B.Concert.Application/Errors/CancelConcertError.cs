using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) => ErrorDefinition.For<CancelConcertError>().NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        TransitionFailure(var error) => error.Definition,
        EscrowRefundFailure(var error) => error.Definition
    };

    [ErrorCode("concert.cancel.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    public partial record TransitionFailure(LifecycleTransitionError Error);
    public partial record EscrowRefundFailure(EscrowRefundError Error);
}
