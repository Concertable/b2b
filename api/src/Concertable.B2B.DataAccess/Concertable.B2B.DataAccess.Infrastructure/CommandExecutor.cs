using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal sealed class CommandExecutor(
    IServiceScopeFactory scopeFactory,
    string connectionString) : ICommandExecutor
{
    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> command,
        CancellationToken ct = default)
        where TService : notnull
    {
        var options = new DbContextOptionsBuilder()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;
        await using var strategyContext = new DbContext(options);
        var strategy = strategyContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var scope = scopeFactory.CreateAsyncScope();
            CommandTransaction? transaction = null;
            CommandTransactionAccessor? accessor = null;

            try
            {
                var services = scope.ServiceProvider;
                accessor = services.GetRequiredService<CommandTransactionAccessor>();
                transaction = await CommandTransaction.BeginAsync(
                    connectionString,
                    services.GetRequiredService<IDbContextAccessor>(),
                    ct);
                accessor.Current = transaction;

                var result = await command(
                    services.GetRequiredService<TService>(),
                    ct);
                if (CommandOutcome.IsFailure(result))
                    transaction.MarkFailed();

                if (transaction.HasFailed)
                {
                    await transaction.RollbackAsync(ct);
                    return result;
                }

                await transaction.FlushAsync(ct);
                await transaction.ValidateAuthorityAsync(ct);
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
        });
    }
}
