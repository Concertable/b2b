using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantContext : ITenantContext, ITenantResolver, IMembershipContext
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITenantRepository repository;

    private Guid? tenantId;
    private TenantRole? role;
    private TenantType? tenantType;
    private bool resolved;

    public TenantContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ITenantRepository repository)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.repository = repository;
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
        if (role is not { } activeRole)
            return false;

        if (requiredPersona is { } persona && tenantType != persona)
            return false;

        return PermissionCatalog.Grants(activeRole, permission);
    }

    public async Task ResolveAsync(CancellationToken ct = default)
    {
        if (resolved || IsHost)
            return;

        resolved = true;

        if (currentUser.Id is not { } userId)
            return;

        var membership = await repository.GetActiveMembershipAsync(userId, ct);
        if (membership is null)
            return;

        tenantId = membership.TenantId;
        role = membership.Role;
        tenantType = membership.Type;
    }
}
