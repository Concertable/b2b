using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class OutboxUnitOfWorkBehavior<TContext> : IOutboxUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    private readonly TContext context;
    private readonly UnitOfWorkRunner unitOfWorkRunner;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;
    private readonly IDbContextAccessor outboxAccessor;

    public OutboxUnitOfWorkBehavior(
        TContext context,
        UnitOfWorkRunner unitOfWorkRunner,
        UnitOfWorkAccessor unitOfWorkAccessor,
        IDbContextAccessor outboxAccessor)
    {
        this.context = context;
        this.unitOfWorkRunner = unitOfWorkRunner;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
        this.outboxAccessor = outboxAccessor;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (this.unitOfWorkAccessor.Current is { } unitOfWork)
        {
            try
            {
                await unitOfWork.EnlistAsync(this.context, cancellationToken);
                var result = await this.RunAsync(action, saveChanges: false, cancellationToken);
                if (ResultOutcome.IsFailure(result))
                    unitOfWork.MarkFailed();
                return result;
            }
            catch
            {
                unitOfWork.MarkFailed();
                throw;
            }
        }

        return await this.unitOfWorkRunner.ExecuteAsync(
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
