using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class CommandTransactionFactory
{
    private readonly string connectionString;
    private readonly CommandTransactionAccessor accessor;
    private readonly IDbContextAccessor outboxAccessor;

    public CommandTransactionFactory(
        string connectionString,
        CommandTransactionAccessor accessor,
        IDbContextAccessor outboxAccessor)
    {
        this.connectionString = connectionString;
        this.accessor = accessor;
        this.outboxAccessor = outboxAccessor;
    }

    public async Task<TResult> ExecuteAsync<TContext, TResult>(
        TContext context,
        Func<Task<TResult>> action,
        CancellationToken ct = default)
        where TContext : DbContext
    {
        if (this.accessor.Current is { } current)
        {
            await current.EnlistAsync(context, ct);
            var nestedResult = await action();
            if (CommandOutcome.IsFailure(nestedResult))
                current.MarkFailed();
            return nestedResult;
        }

        await using var command = await CommandTransaction.BeginAsync(
            this.connectionString,
            this.outboxAccessor,
            ct);
        this.accessor.Current = command;
        try
        {
            await command.EnlistAsync(context, ct);
            var result = await action();
            if (CommandOutcome.IsFailure(result))
                command.MarkFailed();

            if (command.HasFailed)
            {
                await command.RollbackAsync(ct);
                return result;
            }

            await command.FlushAsync(ct);
            await command.ValidateAuthorityAsync(ct);
            await command.CommitAsync(ct);
            return result;
        }
        catch
        {
            await command.RollbackAsync(ct);
            throw;
        }
        finally
        {
            this.accessor.Current = null;
        }
    }
}
