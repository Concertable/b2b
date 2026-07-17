using Concertable.Kernel.Identity;

namespace Concertable.B2B.DataAccess.Infrastructure;

// Design-time only builds the model; no query ever evaluates the tenant filter.
public sealed class DesignTimeTenantContext : ITenantContext
{
    public static readonly DesignTimeTenantContext Instance = new();

    public Guid? TenantId => null;

    public bool IsHost => true;
}
