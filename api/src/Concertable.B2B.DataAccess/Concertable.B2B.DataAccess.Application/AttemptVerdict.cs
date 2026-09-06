using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// What one attempt at an operation decided, once a conflict it recognised has been classified against
/// committed truth. Only <see cref="Transient"/> and <see cref="Recoverable"/> may be replayed, and only
/// where the plumbing gives each attempt its own unit of work; they differ in what happens when the
/// attempt budget is spent — a transient fault is rethrown, a recoverable conflict is reported.
/// </summary>
public abstract record AttemptVerdict<TOutcome>
{
    private AttemptVerdict() { }

    /// <summary>The attempt produced its outcome, whether that outcome is a success or an error. A
    /// classifier returns this where the conflict achieved what the caller wanted anyway.</summary>
    public sealed record Settled(TOutcome Outcome) : AttemptVerdict<TOutcome>;

    /// <summary>Nothing about the world changed — a deadlock, a lock timeout, a dropped connection.
    /// Replaying the identical attempt is free and correct; a spent budget rethrows the fault.</summary>
    public sealed record Transient(DbUpdateException Conflict) : AttemptVerdict<TOutcome>;

    /// <summary>The world did change, and does not forbid the attempt: it was valid and could succeed
    /// now. A spent budget reports <paramref name="Outcome"/> so the caller can ask again.</summary>
    public sealed record Recoverable(TOutcome Outcome) : AttemptVerdict<TOutcome>;

    /// <summary>The state has moved on and forbids the attempt. Reported, never replayed.</summary>
    public sealed record Unrecoverable(TOutcome Outcome) : AttemptVerdict<TOutcome>;
}
