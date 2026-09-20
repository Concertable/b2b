namespace Concertable.B2B.DataAccess.Infrastructure;

internal interface ICommandTransactionCommitter
{
    Task CommitAsync(CommandTransaction transaction, CancellationToken ct);
}

internal sealed class CommandTransactionCommitter : ICommandTransactionCommitter
{
    public Task CommitAsync(CommandTransaction transaction, CancellationToken ct) =>
        transaction.CommitAsync(ct);
}
