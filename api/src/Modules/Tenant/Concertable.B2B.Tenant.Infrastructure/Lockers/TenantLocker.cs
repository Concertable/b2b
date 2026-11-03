using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Lockers;

internal sealed class TenantLocker : ITenantLocker
{
    private readonly ITenantRepository tenantRepository;

    public TenantLocker(ITenantRepository tenantRepository)
    {
        this.tenantRepository = tenantRepository;
    }

    public Task<IReadOnlySet<Guid>> LockExistingAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default) =>
        tenantRepository.GetExistingIdsForShareAsync(tenantIds, ct);
}
