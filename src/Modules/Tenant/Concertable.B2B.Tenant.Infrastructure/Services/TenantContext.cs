using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantContext : ITenantContext, ITenantResolver, IMembershipContext
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMembershipRepository repository;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ITenantContextAccessor accessor;

    public TenantContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IMembershipRepository repository,
        IPermissionCatalog permissionCatalog,
        ITenantContextAccessor accessor)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.repository = repository;
        this.permissionCatalog = permissionCatalog;
        this.accessor = accessor;
    }

    private ActiveTenant? Active => accessor.Resolution?.Tenant;

    public Guid? TenantId => Active?.TenantId;

    public TenantRole? Role => Active?.Role;
    public TenantType? Type => Active?.Type;

    /// <summary>
    /// No HTTP request in scope (worker, outbox dispatcher, event/projection handler) = system caller = filter bypass.
    /// An anonymous HTTP request keeps this <see langword="false"/>, so it fails closed (sees nothing) instead of open.
    /// </summary>
    public bool IsHost => httpContextAccessor.HttpContext is null;

    public bool HasPermission(string permission, TenantType? requiredTenantType = null)
    {
        if (Active is not { } active)
            return false;

        if (requiredTenantType is { } required && active.Type != required)
            return false;

        return permissionCatalog.Grants(active.Type, active.Role, permission);
    }

    public async Task ResolveAsync(CancellationToken ct = default)
    {
        if (accessor.Resolution is not null || IsHost)
            return;

        if (currentUser.Id is not { } userId)
        {
            accessor.Resolution = new TenantResolution(null);
            return;
        }

        var membership = await ResolveMembershipAsync(userId, ct);
        accessor.Resolution = new TenantResolution(
            membership is null
                ? null
                : new ActiveTenant(membership.TenantId, membership.Role, membership.Type));
    }

    /// <summary>
    /// An <c>X-Tenant-Id</c> header names the acting tenant and is validated against the caller's memberships —
    /// a header for a tenant they don't belong to resolves nothing, so the request fails closed. With no header,
    /// a sole membership is the default (keeps every current single-tenant client green); a user with several
    /// must name one, so the request fails closed rather than guess. The switcher sends the header once
    /// multi-membership exists (Phase 6).
    /// </summary>
    private async Task<UserMembership?> ResolveMembershipAsync(Guid userId, CancellationToken ct)
    {
        if (TryGetHeaderTenantId(out var headerTenantId))
            return await repository.GetMembershipAsync(userId, headerTenantId, ct);

        var memberships = await repository.GetMembershipsAsync(userId, ct);
        return memberships is [var sole] ? sole : null;
    }

    private bool TryGetHeaderTenantId(out Guid tenantId)
    {
        tenantId = default;
        return httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(TenantHeaders.TenantId, out var values) is true
            && Guid.TryParse(values.ToString(), out tenantId);
    }
}
