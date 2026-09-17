namespace Concertable.B2B.Privacy.Application.Interfaces;

/// <summary>Re-evaluates every deferred erasure. A subject whose last financial obligation has since settled is
/// erased on this pass; one still obligated stays deferred. Without it a deferred request is recorded and never
/// fulfilled, and the statutory one-calendar-month DSAR deadline passes silently.</summary>
internal interface IDeferredErasureRunner
{
    Task RunAsync(CancellationToken ct = default);
}
