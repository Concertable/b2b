using System.Data.Common;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.B2B.Opportunity.IntegrationTests;

internal sealed class OpportunityLifecycleRaceInterceptor : DbCommandInterceptor, IResettable
{
    private readonly Lock gate = new();
    private Func<Task>? competingChange;
    private TaskCompletionSource lockSubmitted = NewSignal();
    private bool awaitingCompetingLock;

    public void ArmOnce(Func<Task> change)
    {
        lock (gate)
        {
            competingChange = change;
            lockSubmitted = NewSignal();
        }
    }

    public Task WaitForCompetingLockSubmissionAsync() =>
        lockSubmitted.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public void Reset()
    {
        lock (gate)
        {
            competingChange = null;
            awaitingCompetingLock = false;
            lockSubmitted.TrySetCanceled();
            lockSubmitted = NewSignal();
        }
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Func<Task>? change = null;
        lock (gate)
        {
            if (awaitingCompetingLock && IsOpportunityLock(command))
                lockSubmitted.TrySetResult();

            if (competingChange is not null && IsOpportunityUpdate(command))
            {
                change = competingChange;
                competingChange = null;
                awaitingCompetingLock = true;
            }
        }

        try
        {
            if (change is not null)
                await RunDetachedAsync(change);
        }
        finally
        {
            if (change is not null)
            {
                lock (gate)
                    awaitingCompetingLock = false;
            }
        }

        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (awaitingCompetingLock && IsOpportunityLock(command))
                lockSubmitted.TrySetResult();
        }

        return ValueTask.FromResult(result);
    }

    private static bool IsOpportunityUpdate(DbCommand command) =>
        command.CommandText.Contains("UPDATE opportunity.\"Opportunities\"", StringComparison.Ordinal);

    private static bool IsOpportunityLock(DbCommand command) =>
        command.CommandText.Contains("FROM opportunity.\"Opportunities\"", StringComparison.Ordinal)
        && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase);

    private static Task RunDetachedAsync(Func<Task> change)
    {
        using (ExecutionContext.SuppressFlow())
            return Task.Run(change);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
