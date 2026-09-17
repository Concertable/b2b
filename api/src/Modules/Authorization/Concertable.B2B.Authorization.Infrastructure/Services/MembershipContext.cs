using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Authorization.Infrastructure.Services;

internal sealed class MembershipContext : ITenantContext, ITenantResolver, IMembershipContext
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMembershipReadRepository memberships;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly IExecutionScope executionScope;
    private readonly IMembershipContextAccessor accessor;

    public MembershipContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IMembershipReadRepository memberships,
        IPermissionCatalog permissionCatalog,
        IExecutionScope executionScope,
        IMembershipContextAccessor accessor)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.memberships = memberships;
        this.permissionCatalog = permissionCatalog;
        this.executionScope = executionScope;
        this.accessor = accessor;
    }

    public MembershipSnapshot? Membership => accessor.Resolution?.Membership;

    public Guid? TenantId => Membership?.TenantId;

    /// <summary>
    /// Only an explicitly entered execution scope reaches the unfiltered stance. A request-free caller that
    /// established nothing, and an anonymous HTTP request, both leave this <see langword="false"/>, so each
    /// fails closed (sees nothing) instead of open. A header can select no part of it.
    /// </summary>
    public bool IsHost => executionScope.Purpose is not null;

    public bool HasPermission(string permission) =>
        Membership is { } active && permissionCatalog.Grants(active.Role, permission);

    public ResourceAudience AudienceFor(string permission) =>
        Membership is { } active
            ? permissionCatalog.AudienceFor(active.Role, permission)
            : ResourceAudience.None;

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

    /// <summary>
    /// An <c>X-Tenant-Id</c> header names the acting tenant and is validated against the caller's memberships —
    /// a header for a tenant they don't belong to resolves nothing, so the request fails closed. With no header,
    /// a sole membership is the default; a user with several must name one, so the request fails closed rather
    /// than guess.
    /// </summary>
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
