using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class UnitOfWorkRunner
{
    private readonly NpgsqlDataSource dataSource;
    private readonly UnitOfWorkAccessor accessor;
    private readonly IDbContextAccessor outboxAccessor;
    private readonly ITransactionCommitter committer;

    internal UnitOfWorkRunner(
        NpgsqlDataSource dataSource,
        UnitOfWorkAccessor accessor,
        IDbContextAccessor outboxAccessor,
        ITransactionCommitter committer)
    {
        this.dataSource = dataSource;
        this.accessor = accessor;
        this.outboxAccessor = outboxAccessor;
        this.committer = committer;
    }

    public async Task<TResult> RunAsync<TContext, TResult>(
        TContext context,
        Func<Task<TResult>> action,
        CancellationToken ct = default)
        where TContext : DbContext
    {
        if (this.accessor.Current is { } current)
        {
            try
            {
                await current.EnlistAsync(context, ct);
                var nestedResult = await action();
                if (ResultOutcome.IsFailure(nestedResult))
                    current.MarkFailed();
                return nestedResult;
            }
            catch
            {
                current.MarkFailed();
                throw;
            }
        }

        await using var unitOfWork = await UnitOfWork.BeginAsync(
            this.dataSource,
            this.outboxAccessor,
            this.committer,
            ct);
        this.accessor.Current = unitOfWork;
        try
        {
            await unitOfWork.EnlistAsync(context, ct);
            var result = await action();
            var resultFailed = ResultOutcome.IsFailure(result);
            if (resultFailed)
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

            await unitOfWork.CommitAsync(ct);
            return result;
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            this.accessor.Current = null;
        }
    }
}
