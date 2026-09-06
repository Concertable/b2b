using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

/// <summary>
/// Runs an operation and acts on the verdict its conflict classifier returns. The operation itself is
/// retry-free and never learns it is being retried; whether a replay is possible at all is decided by the
/// plumbing, not by an argument at the call site.
/// </summary>
public static class AttemptExtensions
{
    extension<TContext>(IUnitOfWorkBehavior<TContext> behavior)
    {
        /// <summary>
        /// One attempt, always. The change tracker belongs to the ambient dependency-injection scope, so a
        /// replay would run against a dirty tracker — a <see cref="AttemptVerdict{TOutcome}.Transient"/>
        /// fault is rethrown and a <see cref="AttemptVerdict{TOutcome}.Recoverable"/> conflict is reported
        /// for the caller to retry.
        /// </summary>
        public async Task<TOutcome> AttemptAsync<TOutcome>(
            Func<Task<TOutcome>> operation,
            Func<DbUpdateException, bool> isConflict,
            Func<DbUpdateException, Task<AttemptVerdict<TOutcome>>> classify,
            CancellationToken cancellationToken = default)
        {
            var verdict = await behavior.TryExecuteAsync(
                async () => (AttemptVerdict<TOutcome>)new AttemptVerdict<TOutcome>.Settled(await operation()),
                isConflict,
                classify,
                cancellationToken);

            return verdict.Reported();
        }
    }

    extension<TContext>(IUnitOfWorkBoundary<TContext> boundary)
    {
        /// <summary>
        /// Up to <paramref name="attempts"/> attempts, each in its own context and transaction, replaying
        /// while the verdict permits it and the budget remains. A spent budget rethrows a
        /// <see cref="AttemptVerdict{TOutcome}.Transient"/> fault and reports everything else.
        /// </summary>
        public async Task<TOutcome> AttemptAsync<TOutcome>(
            int attempts,
            Func<TContext, Task<TOutcome>> operation,
            Func<DbUpdateException, bool> isConflict,
            Func<DbUpdateException, Task<AttemptVerdict<TOutcome>>> classify,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

            for (var remaining = attempts; ; remaining--)
            {
                var verdict = await boundary.TryExecuteAsync(
                    async context => (AttemptVerdict<TOutcome>)new AttemptVerdict<TOutcome>.Settled(
                        await operation(context)),
                    isConflict,
                    classify,
                    cancellationToken);

                if (remaining <= 1 || !verdict.IsReplayable)
                    return verdict.Reported();
            }
        }
    }

    extension<TOutcome>(AttemptVerdict<TOutcome> verdict)
    {
        private bool IsReplayable =>
            verdict is AttemptVerdict<TOutcome>.Transient or AttemptVerdict<TOutcome>.Recoverable;

        private TOutcome Reported() => verdict switch
        {
            AttemptVerdict<TOutcome>.Settled(var outcome) => outcome,
            AttemptVerdict<TOutcome>.Recoverable(var outcome) => outcome,
            AttemptVerdict<TOutcome>.Unrecoverable(var outcome) => outcome,
            AttemptVerdict<TOutcome>.Transient(var conflict) => throw conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
        };
    }
}
