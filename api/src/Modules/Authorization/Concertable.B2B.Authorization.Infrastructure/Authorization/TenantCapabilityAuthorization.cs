using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class TenantCapabilityAuthorization(
    ActorAuthoritySession actor,
    TenantCapabilityRegistry registry) : ITenantCapabilityAuthorization
{
    public async Task<AuthorizationDecision> CheckAsync(TenantPermission permission, CancellationToken ct = default)
    {
        if (!registry.Contains(permission))
            return AuthorizationDecision.Denied;

        var resolution = await actor.CheckAsync(ct);
        return resolution.Decision != AuthorizationDecision.Allowed
            ? resolution.Decision
            : resolution.Authority!.Actor.AudienceFor(permission) == ResourceAudience.TenantResources
                ? AuthorizationDecision.Allowed
                : AuthorizationDecision.Denied;
    }

    public async Task<AuthorizationDecision> RequireAsync(TenantPermission permission, CancellationToken ct = default)
    {
        var resolution = await actor.RequireAsync(ct);
        if (resolution.Decision != AuthorizationDecision.Allowed)
            return resolution.Decision;

        if (!registry.Contains(permission)
            || resolution.Authority!.Actor.AudienceFor(permission) != ResourceAudience.TenantResources)
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        return AuthorizationDecision.Allowed;
    }
}
