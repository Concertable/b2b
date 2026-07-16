namespace Concertable.B2B.Concert.Application.Workflow.Executors;

/// <summary>
/// The outcome of a finish attempt, carried on the success side of the executor's <c>Result</c> (a genuine failure
/// stays on the <c>Result</c>'s error side). <see cref="DeferredPendingTaxCompliance"/> is a first-class no-op: the
/// payee's tax identity is not yet complete, so the concert is left un-transitioned and unpaid and the next hourly
/// sweep retries — it self-heals the moment the payee completes their details, and must be logged as neither an
/// error nor a completion.
/// </summary>
internal enum SettlementOutcome
{
    Settled,
    DeferredPendingTaxCompliance,
}
