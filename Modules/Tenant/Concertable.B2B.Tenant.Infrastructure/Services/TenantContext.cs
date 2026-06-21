using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantContext : ITenantContext, ITenantResolver, IMembershipContext
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITenantRepository repository;
    private readonly IPermissionCatalog permissionCatalog;

    private Guid? tenantId;
    private TenantRole? role;
    private TenantType? tenantType;
    private bool resolved;

    public TenantContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ITenantRepository repository,
        IPermissionCatalog permissionCatalog)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.repository = repository;
        this.permissionCatalog = permissionCatalog;
    }

    public Guid? TenantId => tenantId;

    public TenantRole? Role => role;

    /// <summary>
    /// No HTTP request in scope (worker, outbox dispatcher, event/projection handler) = system caller = filter bypass.
    /// An anonymous HTTP request keeps this <see langword="false"/>, so it fails closed (sees nothing) instead of open.
    /// </summary>
    public bool IsHost => httpContextAccessor.HttpContext is null;

    public bool HasPermission(string permission, TenantType? requiredPersona = null)
    {
        if (role is not { } activeRole || tenantType is not { } persona)
            return false;

        if (requiredPersona is { } required && persona != required)
            return false;

        return permissionCatalog.Grants(persona, activeRole, permission);
    }

    public async Task ResolveAsync(CancellationToken ct = default)
    {
        if (resolved || IsHost)
            return;

        resolved = true;

        if (currentUser.Id is not { } userId)
            return;

        var membership = await ResolveMembershipAsync(userId, ct);
        if (membership is null)
            return;

        tenantId = membership.TenantId;
        role = membership.Role;
        tenantType = membership.Type;
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
