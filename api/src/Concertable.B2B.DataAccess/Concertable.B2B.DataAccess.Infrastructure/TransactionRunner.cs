using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal sealed class TransactionRunner(
    IServiceScopeFactory scopeFactory,
    NpgsqlDataSource dataSource,
    ITransactionCommitter committer) : ITransactionRunner
{
    public async Task<TResult> RunAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
        where TService : notnull
        => await RunCoreAsync(operation, null, null, ct);

    public async Task<TResult> RunAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        Func<TService, TResult, CancellationToken, Task<bool>> validateAuthority,
        Func<TResult> authorityFailure,
        CancellationToken ct = default)
        where TService : notnull
        => await RunCoreAsync(operation, validateAuthority, authorityFailure, ct);

    private async Task<TResult> RunCoreAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        Func<TService, TResult, CancellationToken, Task<bool>>? validateAuthority,
        Func<TResult>? authorityFailure,
        CancellationToken ct)
        where TService : notnull
    {
        var scope = scopeFactory.CreateAsyncScope();
        UnitOfWork? unitOfWork = null;
        UnitOfWorkAccessor? accessor = null;

        try
        {
            var services = scope.ServiceProvider;
            accessor = services.GetRequiredService<UnitOfWorkAccessor>();
            unitOfWork = await UnitOfWork.BeginAsync(
                dataSource,
                services.GetRequiredService<IDbContextAccessor>(),
                committer,
                ct);
            accessor.Current = unitOfWork;

            var service = services.GetRequiredService<TService>();
            var result = await operation(service, ct);
            if (ResultOutcome.IsFailure(result))
                unitOfWork.MarkFailed();

            if (unitOfWork.HasFailed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return unitOfWork.FailedResult(result);
            }

            await unitOfWork.FlushAsync(ct);
            if (unitOfWork.IsAuthorityManaged && unitOfWork.HasFailed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return unitOfWork.FailedResult(result);
            }

            await unitOfWork.ValidateAuthorityAsync(ct);
            if (unitOfWork.IsAuthorityManaged && unitOfWork.HasFailed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return unitOfWork.FailedResult(result);
            }
            if (validateAuthority is not null
                && !await validateAuthority(service, result, ct))
            {
                await unitOfWork.RollbackAsync(ct);
                return (authorityFailure
                    ?? throw new InvalidOperationException("An authority failure result is required."))();
            }
            if (unitOfWork.IsAuthorityManaged && unitOfWork.HasFailed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return unitOfWork.FailedResult(result);
            }

            await unitOfWork.CommitAsync(ct);
            return result;
        }
        catch
        {
            if (unitOfWork is not null)
                await unitOfWork.RollbackAsync(ct);
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
                if (unitOfWork is not null)
                    await unitOfWork.DisposeAsync();
            }
        }
    }
}
