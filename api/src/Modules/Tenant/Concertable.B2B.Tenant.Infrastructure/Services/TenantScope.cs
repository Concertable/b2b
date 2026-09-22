using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantScope : ITenantScope
{
    private readonly ITenantContextAccessor accessor;

    public TenantScope(ITenantContextAccessor accessor)
    {
        this.accessor = accessor;
    }

    public IDisposable As(Guid tenantId)
    {
        var previous = accessor.Resolution;
        accessor.Resolution = new TenantResolution(new ActiveTenant(tenantId, null, null));
        return new Restore(accessor, previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly ITenantContextAccessor accessor;
        private readonly TenantResolution? previous;

        public Restore(ITenantContextAccessor accessor, TenantResolution? previous)
        {
            this.accessor = accessor;
            this.previous = previous;
        }

        public void Dispose() => accessor.Resolution = previous;
    }
}
