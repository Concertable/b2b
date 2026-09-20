using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal interface ICommandTransactionCommitter
{
    Task CommitAsync(NpgsqlTransaction transaction, CancellationToken ct);
}

internal sealed class CommandTransactionCommitter : ICommandTransactionCommitter
{
    public Task CommitAsync(NpgsqlTransaction transaction, CancellationToken ct) =>
        transaction.CommitAsync(ct);
}
