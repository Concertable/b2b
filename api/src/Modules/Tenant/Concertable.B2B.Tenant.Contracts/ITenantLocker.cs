namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantLocker
{
    Task<IReadOnlySet<Guid>> LockExistingAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default);
}
