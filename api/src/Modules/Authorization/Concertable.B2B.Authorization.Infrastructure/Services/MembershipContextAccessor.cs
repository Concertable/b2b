using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Authorization.Infrastructure.Services;

internal sealed record ActiveMembership(Guid TenantId, Guid UserId, TenantRole Role, long AuthorizationVersion);

/// <summary>
/// A resolution that has already happened. <see cref="Membership"/> is <see langword="null"/> when the caller
/// has no usable membership — resolved, and deliberately nothing, so the request fails closed without
/// re-querying.
/// </summary>
internal sealed record MembershipResolution(ActiveMembership? Membership);

/// <summary>
/// Carries the resolved membership for the current request. It belongs to the request, not to a
/// dependency-injection scope: memoizing it per scope answers "no tenant" in every scope the middleware did
/// not itself create, so any operation that opens one sees an unresolved tenant and every filtered read comes
/// back empty. Storage is this type's business — callers only see the request.
/// </summary>
internal interface IMembershipContextAccessor
{
    MembershipResolution? Resolution { get; set; }
}

internal sealed class MembershipContextAccessor : IMembershipContextAccessor
{
    private const string ItemKey = "Concertable.Authorization.Resolution";

    private readonly IHttpContextAccessor httpContextAccessor;

    public MembershipContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public MembershipResolution? Resolution
    {
        get => httpContextAccessor.HttpContext is { } http && http.Items.TryGetValue(ItemKey, out var value)
            ? value as MembershipResolution
            : null;
        set
        {
            if (httpContextAccessor.HttpContext is not { } http)
                throw new InvalidOperationException(
                    "A membership resolution has no request to belong to. Established execution scopes resolve nothing.");

            http.Items[ItemKey] = value;
        }
    }
}
