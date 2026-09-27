using Concertable.B2B.Tenant.Infrastructure;

namespace Concertable.B2B.Tenant.UnitTests;

internal sealed class ImmediateUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default) =>
        action();

    public Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default) =>
        action();
}
