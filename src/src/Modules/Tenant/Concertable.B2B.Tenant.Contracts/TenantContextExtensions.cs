using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Contracts;

public static class TenantContextExtensions
{
    /// <summary>The active tenant id, or a fail-closed 403 when the request has no resolved tenant.</summary>
    public static Guid GetTenantId(this ITenantContext context)
        => context.TenantId ?? throw new ForbiddenException("No active tenant for the current user.");
}
