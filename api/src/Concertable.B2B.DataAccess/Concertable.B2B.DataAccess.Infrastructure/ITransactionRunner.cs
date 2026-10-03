namespace Concertable.B2B.DataAccess.Infrastructure;

public interface ITransactionRunner
{
    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
        where TService : notnull;

    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        Func<TService, TResult, CancellationToken, Task<bool>> validateAuthority,
        Func<TResult> authorityFailure,
        CancellationToken ct = default)
        where TService : notnull;
}
