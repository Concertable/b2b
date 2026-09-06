using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed record ActiveTenant(Guid TenantId, TenantRole Role, TenantType Type);

/// <summary>
/// A resolution that has already happened. <see cref="Tenant"/> is <see langword="null"/> when the caller
/// has no usable membership — resolved, and deliberately nothing, so the request fails closed without
/// re-querying.
/// </summary>
internal sealed record TenantResolution(ActiveTenant? Tenant);

/// <summary>
/// Carries the resolved tenant for the current request. The tenant belongs to the request, not to a
/// dependency-injection scope: memoizing it per scope answers "no tenant" in every scope the middleware did
/// not itself create, so any operation that opens one sees an unresolved tenant and every filtered read comes
/// back empty. Storage is this type's business — callers only see the request.
/// </summary>
internal interface ITenantContextAccessor
{
    TenantResolution? Resolution { get; set; }
}

internal sealed class TenantContextAccessor : ITenantContextAccessor
{
    private const string ItemKey = "Concertable.Tenant.Resolution";

    private readonly IHttpContextAccessor httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantResolution? Resolution
    {
        get => httpContextAccessor.HttpContext is { } http && http.Items.TryGetValue(ItemKey, out var value)
            ? value as TenantResolution
            : null;
        set
        {
            if (httpContextAccessor.HttpContext is not { } http)
                throw new InvalidOperationException(
                    "A tenant resolution has no request to belong to. Host callers resolve nothing.");

            http.Items[ItemKey] = value;
        }
    }
}
