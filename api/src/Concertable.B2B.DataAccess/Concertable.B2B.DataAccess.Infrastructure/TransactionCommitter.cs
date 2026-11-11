using System.Data.Common;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal interface ITransactionCommitter
{
    Task CommitAsync(DbTransaction transaction, CancellationToken ct);
}

internal sealed class TransactionCommitter : ITransactionCommitter
{
    public Task CommitAsync(DbTransaction transaction, CancellationToken ct) =>
        transaction.CommitAsync(ct);
}
