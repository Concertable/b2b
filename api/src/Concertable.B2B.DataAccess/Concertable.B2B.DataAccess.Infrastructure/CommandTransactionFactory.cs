using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class CommandTransactionFactory
{
    private readonly NpgsqlDataSource dataSource;
    private readonly CommandTransactionAccessor accessor;
    private readonly IDbContextAccessor outboxAccessor;
    private readonly ICommandTransactionCommitter committer;

    internal CommandTransactionFactory(
        NpgsqlDataSource dataSource,
        CommandTransactionAccessor accessor,
        IDbContextAccessor outboxAccessor,
        ICommandTransactionCommitter committer)
    {
        this.dataSource = dataSource;
        this.accessor = accessor;
        this.outboxAccessor = outboxAccessor;
        this.committer = committer;
    }

    public async Task<TResult> ExecuteAsync<TContext, TResult>(
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
                if (CommandOutcome.IsFailure(nestedResult))
                    current.MarkFailed();
                return nestedResult;
            }
            catch
            {
                current.MarkFailed();
                throw;
            }
        }

        await using var command = await CommandTransaction.BeginAsync(
            this.dataSource,
            this.outboxAccessor,
            this.committer,
            ct);
        this.accessor.Current = command;
        try
        {
            await command.EnlistAsync(context, ct);
            var result = await action();
            var resultFailed = CommandOutcome.IsFailure(result);
            if (resultFailed)
                command.MarkFailed();

            if (command.HasFailed)
            {
                await command.RollbackAsync(CancellationToken.None);
                if (!resultFailed)
                    throw new InvalidOperationException(
                        "A nested command failed after the outer action returned success.");
                return result;
            }

            await command.FlushAsync(ct);
            await command.ValidateAuthorityAsync(ct);
            await command.CommitAsync(ct);
            return result;
        }
        catch
        {
            await command.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            this.accessor.Current = null;
        }
    }
}
