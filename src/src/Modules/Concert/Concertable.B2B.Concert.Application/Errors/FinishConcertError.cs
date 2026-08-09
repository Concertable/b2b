using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record FinishConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) => ErrorDefinition.For<FinishConcertError>().NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        ConcertNotEnded => ErrorDefinition.For<FinishConcertError>().Invalid<ConcertNotEnded>(
            "The concert cannot be finished before it has ended."),
        TransitionFailure(var error) => error.Definition,
        ManagerPaymentFailure(var error) => error.Definition,
        EscrowReleaseFailure(var error) => error.Definition
    };

    [ErrorCode("concert.finish.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.finish.not_ended")]
    public partial record ConcertNotEnded;

    public partial record TransitionFailure(LifecycleTransitionError Error);
    public partial record ManagerPaymentFailure(ManagerPaymentError Error);
    public partial record EscrowReleaseFailure(EscrowReleaseError Error);
}
