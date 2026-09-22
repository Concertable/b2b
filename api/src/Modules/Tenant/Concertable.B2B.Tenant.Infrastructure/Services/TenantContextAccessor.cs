using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

/// <summary>
/// The tenant a caller is acting as. <see cref="Role"/> and <see cref="Type"/> are <see langword="null"/> for a
/// request-less caller that states a tenant to write as — it has an authority boundary but no membership, so
/// every permission question about it fails closed.
/// </summary>
internal sealed record ActiveTenant(Guid TenantId, TenantRole? Role, TenantType? Type);

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

    // Where a request-less caller's tenant lives: a worker, dispatcher or seeder has no HttpContext.Items to
    // hang a resolution on. Async-local so it follows one operation's await chain and does not leak into the
    // concurrent operations sharing this singleton.
    private static readonly AsyncLocal<TenantResolution?> Ambient = new();

    private readonly IHttpContextAccessor httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public TenantResolution? Resolution
    {
        get => httpContextAccessor.HttpContext is { } http
            ? http.Items.TryGetValue(ItemKey, out var value) ? value as TenantResolution : null
            : Ambient.Value;
        set
        {
            if (httpContextAccessor.HttpContext is { } http)
                http.Items[ItemKey] = value;
            else
                Ambient.Value = value;
        }
    }
}
