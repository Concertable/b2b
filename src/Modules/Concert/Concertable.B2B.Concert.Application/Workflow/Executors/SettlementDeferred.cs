using FluentResults;

namespace Concertable.B2B.Concert.Application.Workflow.Executors;

/// <summary>
/// Marks a finish that intentionally did not settle: the payee is not yet DAC7-complete, so the concert is
/// left un-transitioned and the next hourly completion sweep retries — it self-heals the moment the seller
/// completes their tax details. A success <em>reason</em>, not an error: a deferral is expected and must not
/// spam the error log on every sweep, and it is not a completion, so the runner does not log it as one.
/// </summary>
internal sealed class SettlementDeferred : ISuccess
{
    public string Message => "Settlement deferred: payee is not DAC7-complete.";
    public Dictionary<string, object> Metadata { get; } = new();
}
