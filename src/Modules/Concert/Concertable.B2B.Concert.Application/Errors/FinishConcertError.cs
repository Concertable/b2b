using Concertable.B2B.Concert.Domain.State;
using Concertable.Payment.Contracts.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record FinishConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        ConcertNotEnded => ErrorDefinition.Invalid<ConcertNotEnded>(
            "The concert cannot be finished before it has ended."),
        DoorRevenueRequired => ErrorDefinition.Invalid<DoorRevenueRequired>(
            "Door revenue must be declared before the concert can be finished."),
        InvalidState(var state) => ErrorDefinition.Invalid<InvalidState>(
            $"A concert in {state} cannot be finished."),
        ManagerPaymentFailure(var error) => error.Definition,
        EscrowReleaseFailure(var error) => error.Definition
    };

    [ErrorCode("concert.finish.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.finish.not_ended")]
    public partial record ConcertNotEnded;

    [ErrorCode("concert.finish.door_revenue_required")]
    public partial record DoorRevenueRequired;

    [ErrorCode("concert.finish.invalid_state")]
    public partial record InvalidState(ConcertState State);

    public partial record ManagerPaymentFailure(ManagerPaymentError Error);
    public partial record EscrowReleaseFailure(EscrowReleaseError Error);
}
