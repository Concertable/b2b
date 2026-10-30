using System.Data.Common;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class CommandPauseInterceptor : DbCommandInterceptor, IResettable
{
    private readonly Lock gate = new();
    private Func<DbCommand, bool>? match;
    private TaskCompletionSource reached = NewSignal();
    private TaskCompletionSource released = NewSignal();

    public void Arm(Func<DbCommand, bool> matches)
    {
        lock (gate)
        {
            match = matches;
            reached = NewSignal();
            released = NewSignal();
        }
    }

    public Task WaitUntilPausedAsync()
    {
        lock (gate)
            return reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    public void Release()
    {
        lock (gate)
            released.TrySetResult();
    }

    public void Reset()
    {
        lock (gate)
        {
            match = null;
            released.TrySetResult();
            reached = NewSignal();
            released = NewSignal();
        }
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await PauseIfMatchedAsync(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await PauseIfMatchedAsync(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await PauseIfMatchedAsync(command, cancellationToken);
        return result;
    }

    private Task PauseIfMatchedAsync(DbCommand command, CancellationToken ct)
    {
        Task release;
        lock (gate)
        {
            if (match is null || !match(command))
                return Task.CompletedTask;
            match = null;
            reached.TrySetResult();
            release = released.Task;
        }

        return release.WaitAsync(ct);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
