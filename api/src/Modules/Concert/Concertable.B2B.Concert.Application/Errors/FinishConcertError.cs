using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;
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
        InvalidTransition(var error) => ErrorDefinition.Conflict<InvalidTransition>(
            $"A concert in {error.Current} cannot be finished."),
        SettlementChargeFailure(var error) => error.Definition,
        SettlementCommissionFailure(var error) => error.Definition,
        SettlementOperationConflict => ErrorDefinition.Conflict<SettlementOperationConflict>(
            "The settlement operation identity conflicts with a payment Payment already recorded."),
        PaymentCommitmentFailure(var error) => error.Definition,
        PaymentAuthenticationRequired => ErrorDefinition.PaymentRequired<PaymentAuthenticationRequired>(
            "The committed payment method needs the payer to authenticate before settlement can complete."),
        EscrowReleaseFailure(var error) => error.Definition
    };

    [ErrorCode("concert.finish.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.finish.not_ended")]
    public partial record ConcertNotEnded;

    [ErrorCode("concert.finish.door_revenue_required")]
    public partial record DoorRevenueRequired;

    [ErrorCode("concert.finish.invalid_state")]
    public partial record InvalidTransition(TransitionError<ConcertState, ConcertTrigger> Error);

    public partial record SettlementChargeFailure(PaymentError Error);

    public partial record SettlementCommissionFailure(CommissionError Error);

    [ErrorCode("concert.finish.settlement_operation_conflict")]
    public partial record SettlementOperationConflict;

    /// <summary>The commitment itself is unusable, so recovery needs a fresh payment-method setup.</summary>
    public partial record PaymentCommitmentFailure(PaymentOperationError Error);

    /// <summary>The commitment stands but needs the payer back on-session against the same operation
    /// reference; recovery re-enters the existing operation rather than setting up a new method.</summary>
    [ErrorCode("concert.finish.payment_authentication_required")]
    public partial record PaymentAuthenticationRequired;

    public partial record EscrowReleaseFailure(EscrowReleaseOperationError Error);
}
