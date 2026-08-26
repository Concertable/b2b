namespace Concertable.B2B.Concert.Application.Workflow.Executors;

/// <summary>
/// The outcome of a finish attempt, carried on the success side of the executor's <c>Result</c> (a genuine failure
/// stays on the <c>Result</c>'s error side). The deferral cases are first-class no-ops: the concert is left
/// un-transitioned and unpaid and the next hourly sweep retries, self-healing once the missing precondition lands —
/// so each must be logged as neither an error nor a completion. <see cref="DeferredPendingTaxCompliance"/> waits on a
/// party's jurisdiction-complete tax identity; <see cref="DeferredPendingVerification"/> waits on a party's tenant
/// verification being approved; <see cref="DeferredPendingSelfBillingAgreement"/> waits on the supplier
/// holding a current self-billing agreement, without which no self-billed invoice may be raised in their name.
/// </summary>
internal enum SettlementOutcome
{
    Settled,
    DeferredPendingTaxCompliance,
    DeferredPendingVerification,
    DeferredPendingSelfBillingAgreement,
}
