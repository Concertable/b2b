using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class CommandUnitOfWorkBehavior<TContext> : IUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    private readonly TContext context;
    private readonly CommandTransactionFactory transactions;
    private readonly CommandTransactionAccessor accessor;

    public CommandUnitOfWorkBehavior(
        TContext context,
        CommandTransactionFactory transactions,
        CommandTransactionAccessor accessor)
    {
        this.context = context;
        this.transactions = transactions;
        this.accessor = accessor;
    }

    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default) =>
        this.transactions.ExecuteAsync(this.context, action, cancellationToken);

    public Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default) =>
        this.ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);

    public async Task<TResult> TryExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<TResult>> onExpectedFailure,
        CancellationToken cancellationToken = default)
    {
        if (this.accessor.Current is not null)
            return await this.ExecuteAsync(action, cancellationToken);

        try
        {
            return await this.ExecuteAsync(action, cancellationToken);
        }
        catch (DbUpdateException exception) when (isExpected(exception))
        {
            return await onExpectedFailure(exception);
        }
    }
}
