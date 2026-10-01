using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal sealed class TransactionRunner(
    IServiceScopeFactory scopeFactory,
    NpgsqlDataSource dataSource,
    ICommandTransactionCommitter committer) : ITransactionRunner
{
    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
        where TService : notnull
        => await ExecuteCoreAsync(operation, null, null, ct);

    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        Func<TService, TResult, CancellationToken, Task<bool>> validateAuthority,
        Func<TResult> authorityFailure,
        CancellationToken ct = default)
        where TService : notnull
        => await ExecuteCoreAsync(operation, validateAuthority, authorityFailure, ct);

    private async Task<TResult> ExecuteCoreAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        Func<TService, TResult, CancellationToken, Task<bool>>? validateAuthority,
        Func<TResult>? authorityFailure,
        CancellationToken ct)
        where TService : notnull
    {
        var scope = scopeFactory.CreateAsyncScope();
        CommandTransaction? transaction = null;
        CommandTransactionAccessor? accessor = null;

        try
        {
            var services = scope.ServiceProvider;
            accessor = services.GetRequiredService<CommandTransactionAccessor>();
            transaction = await CommandTransaction.BeginAsync(
                dataSource,
                services.GetRequiredService<IDbContextAccessor>(),
                committer,
                ct);
            accessor.Current = transaction;

            var service = services.GetRequiredService<TService>();
            var result = await operation(service, ct);
            if (CommandOutcome.IsFailure(result))
                transaction.MarkFailed();

            if (transaction.HasFailed)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return transaction.FailedResult(result);
            }

            await transaction.FlushAsync(ct);
            if (transaction.IsAuthorityManaged && transaction.HasFailed)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return transaction.FailedResult(result);
            }

            await transaction.ValidateAuthorityAsync(ct);
            if (transaction.IsAuthorityManaged && transaction.HasFailed)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return transaction.FailedResult(result);
            }
            if (validateAuthority is not null
                && !await validateAuthority(service, result, ct))
            {
                await transaction.RollbackAsync(ct);
                return (authorityFailure
                    ?? throw new InvalidOperationException("An authority failure result is required."))();
            }
            if (transaction.IsAuthorityManaged && transaction.HasFailed)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return transaction.FailedResult(result);
            }

            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (accessor is not null)
                accessor.Current = null;
            try
            {
                await scope.DisposeAsync();
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }
    }
}
