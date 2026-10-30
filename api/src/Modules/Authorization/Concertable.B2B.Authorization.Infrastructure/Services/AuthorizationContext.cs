using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Authorization.Infrastructure.Services;

internal sealed class AuthorizationContext : ITenantContext, ITenantResolver, IAuthorizationContext
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMembershipReadRepository memberships;
    private readonly IAuthorizationContextAccessor accessor;

    public AuthorizationContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IMembershipReadRepository memberships,
        IAuthorizationContextAccessor accessor)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.memberships = memberships;
        this.accessor = accessor;
    }

    public MembershipSnapshot? Membership => accessor.Resolution?.Membership;

    public Guid? TenantId => Membership?.TenantId;

    public bool HasPermission(TenantPermission permission) =>
        Membership is { } active && active.HasPermission(permission);

    public ResourceCoverage CoverageFor(TenantPermission permission) =>
        Membership?.CoverageFor(permission) ?? ResourceCoverage.None;

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext is null || accessor.Resolution is not null)
            return;

        if (currentUser.Id is not { } userId)
        {
            accessor.Resolution = new MembershipResolution(null);
            return;
        }

        accessor.Resolution = new MembershipResolution(await ResolveMembershipAsync(userId, cancellationToken));
    }

    private async Task<MembershipSnapshot?> ResolveMembershipAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (TryGetHeaderTenantId(out var headerTenantId))
            return await memberships.GetSnapshotByUserIdAndTenantIdAsync(userId, headerTenantId, cancellationToken);

        if (HasTenantHeader())
            throw new MalformedTenantHeaderException();

        var all = await memberships.GetSnapshotsByUserIdAsync(userId, cancellationToken);
        return all is [var sole] ? sole : null;
    }

    private bool HasTenantHeader() =>
        httpContextAccessor.HttpContext?.Request.Headers.ContainsKey(TenantHeaders.TenantId) is true;

    private bool TryGetHeaderTenantId(out Guid tenantId)
    {
        tenantId = default;
        return httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(TenantHeaders.TenantId, out var values) is true
            && Guid.TryParse(values.ToString(), out tenantId)
            && tenantId != Guid.Empty;
    }
}
