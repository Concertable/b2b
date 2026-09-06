using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed record ActiveTenant(Guid TenantId, TenantRole Role, TenantType Type);

/// <summary>
/// A resolution that has already happened. <see cref="Tenant"/> is <see langword="null"/> when the caller
/// has no usable membership — resolved, and deliberately nothing, so the request fails closed without
/// re-querying.
/// </summary>
internal sealed record TenantResolution(ActiveTenant? Tenant);

/// <summary>
/// Carries the resolved tenant for the current operation. Backed by <see cref="AsyncLocal{T}"/> rather than
/// by scoped state, because the tenant belongs to the request, not to a dependency-injection scope: a scoped
/// memo answers "no tenant" in every scope the middleware did not itself create, so any operation that opens
/// one sees an unresolved tenant and every filtered read comes back empty.
/// </summary>
internal interface ITenantContextAccessor
{
    TenantResolution? Resolution { get; set; }
}

internal sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly AsyncLocal<Holder> current = new();

    public TenantResolution? Resolution
    {
        get => current.Value?.Resolution;
        set
        {
            // Clear through the holder rather than by nulling the AsyncLocal, so execution contexts that
            // already captured it observe the clear.
            if (current.Value is { } holder)
                holder.Resolution = null;

            if (value is not null)
                current.Value = new Holder { Resolution = value };
        }
    }

    private sealed class Holder
    {
        public TenantResolution? Resolution;
    }
}
