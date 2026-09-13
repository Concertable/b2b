using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Contracts;

public static class TenantContextExtensions
{
    public static Guid GetTenantId(this ITenantContext context)
        => context.TenantId ?? throw new InvalidOperationException(
            "The operation requires an active tenant context.");
}
