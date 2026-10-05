using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionAuthorization : IPermissionAuthorization
{
    private readonly ActorAuthoritySession actor;

    public PermissionAuthorization(ActorAuthoritySession actor)
    {
        this.actor = actor;
    }
    public async Task<AuthorizationDecision> CheckAsync(
        TenantPermission permission,
        ResourceAudience? requiredAudience = null,
        CancellationToken ct = default)
    {
        if (!TenantPermission.All.Contains(permission)
            || requiredAudience is not null and not ResourceAudience.AssignedResources
                and not ResourceAudience.TenantResources)
            return AuthorizationDecision.Denied;

        var resolution = await this.actor.CheckAsync(ct);
        if (resolution.Decision != AuthorizationDecision.Allowed)
            return resolution.Decision;

        var audience = resolution.Authority!.Actor.AudienceFor(permission);
        return audience is ResourceAudience.AssignedResources or ResourceAudience.TenantResources
            && (requiredAudience is null
                || requiredAudience == ResourceAudience.AssignedResources
                || audience == ResourceAudience.TenantResources)
            ? AuthorizationDecision.Allowed
            : AuthorizationDecision.Denied;
    }

    public async Task<AuthorizationDecision> RequireAsync(
        TenantPermission permission,
        ResourceAudience requiredAudience,
        CancellationToken ct = default)
    {
        var resolution = await this.actor.RequireAsync(ct);
        if (resolution.Decision != AuthorizationDecision.Allowed)
            return resolution.Decision;

        var audience = resolution.Authority!.Actor.AudienceFor(permission);
        if (!TenantPermission.All.Contains(permission)
            || requiredAudience is not ResourceAudience.AssignedResources
                and not ResourceAudience.TenantResources
            || audience is not ResourceAudience.AssignedResources
                and not ResourceAudience.TenantResources
            || requiredAudience == ResourceAudience.TenantResources
                && audience != ResourceAudience.TenantResources)
            return this.actor.Fail(AuthorizationDecision.Denied).Decision;

        return AuthorizationDecision.Allowed;
    }
}
