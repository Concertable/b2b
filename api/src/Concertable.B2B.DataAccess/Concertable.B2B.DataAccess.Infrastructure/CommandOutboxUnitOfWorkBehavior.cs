using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class CommandOutboxUnitOfWorkBehavior<TContext> : IOutboxUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    private readonly TContext context;
    private readonly CommandTransactionFactory transactions;
    private readonly CommandTransactionAccessor commandAccessor;
    private readonly IDbContextAccessor outboxAccessor;

    public CommandOutboxUnitOfWorkBehavior(
        TContext context,
        CommandTransactionFactory transactions,
        CommandTransactionAccessor commandAccessor,
        IDbContextAccessor outboxAccessor)
    {
        this.context = context;
        this.transactions = transactions;
        this.commandAccessor = commandAccessor;
        this.outboxAccessor = outboxAccessor;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (this.commandAccessor.Current is { } command)
        {
            await command.EnlistAsync(this.context, cancellationToken);
            return await this.RunAsync(action, saveChanges: false, cancellationToken);
        }

        return await this.transactions.ExecuteAsync(
            this.context,
            () => this.RunAsync(action, saveChanges: false, cancellationToken),
            cancellationToken);
    }

    public Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default) =>
        this.ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);

    private async Task<TResult> RunAsync<TResult>(
        Func<Task<TResult>> action,
        bool saveChanges,
        CancellationToken ct)
    {
        var previous = this.outboxAccessor.Context;
        this.outboxAccessor.Context = this.context;
        try
        {
            var result = await action();
            if (saveChanges)
                await this.context.SaveChangesAsync(ct);
            return result;
        }
        finally
        {
            this.outboxAccessor.Context = previous;
        }
    }
}
