namespace Concertable.B2B.DataAccess.Infrastructure;

public interface ICommandExecutor
{
    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> command,
        CancellationToken ct = default)
        where TService : notnull;
}
