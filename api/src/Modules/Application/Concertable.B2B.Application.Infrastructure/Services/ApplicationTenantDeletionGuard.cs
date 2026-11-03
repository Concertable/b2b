using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationTenantDeletionGuard : ITenantDeletionGuard
{
    private readonly IApplicationPrivilegedRepository applicationPrivilegedRepository;

    public ApplicationTenantDeletionGuard(IApplicationPrivilegedRepository applicationPrivilegedRepository)
    {
        this.applicationPrivilegedRepository = applicationPrivilegedRepository;
    }

    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        await applicationPrivilegedRepository.AcquireTenantApplicationsExclusiveLockAsync(tenantId, ct);
        return await applicationPrivilegedRepository.ExistsLiveByTenantIdAsync(tenantId, ct);
    }
}
