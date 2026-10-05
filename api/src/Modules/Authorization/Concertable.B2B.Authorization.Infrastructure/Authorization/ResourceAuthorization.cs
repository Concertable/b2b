using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class ResourceAuthorization(
    ActorAuthoritySession actor,
    IAuthorizationContext authorizationContext,
    ResourceBindingRegistry registry,
    TimeProvider clock) : IResourceAuthorization
{
    public async Task<AuthorizationDecision> CheckAsync(AuthorizationRequest request, CancellationToken ct = default)
    {
        if (!registry.TryResolve(request, out var binding))
            return AuthorizationDecision.Denied;

        var resolution = await actor.CheckAsync(ct);
        if (resolution.Decision != AuthorizationDecision.Allowed)
            return resolution.Decision;

        var authority = resolution.Authority!;
        if (!authority.Actor.HasPermission(request.Permission))
            return AuthorizationDecision.Denied;

        var now = clock.GetUtcNow();
        var decision = await registry.Evaluator(request.Resource.Kind).CheckAsync(
            request, binding!, authority.Actor, now, ct);
        return decision.IsAllowed && HasValidGrants(binding!, decision, authority.Actor, now)
            ? AuthorizationDecision.Allowed
            : AuthorizationDecision.Denied;
    }

    public async Task<AuthorizationDecision> RequireAsync(AuthorizationRequest request, CancellationToken ct = default)
    {
        var resolution = await actor.RequireAsync(ct);
        if (resolution.Decision != AuthorizationDecision.Allowed)
            return resolution.Decision;

        if (!registry.TryResolve(request, out var binding))
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        var authority = resolution.Authority!;
        if (!authority.Actor.HasPermission(request.Permission))
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        var evaluator = registry.Evaluator(request.Resource.Kind);
        var now = clock.GetUtcNow();
        var decision = await evaluator.RequireAsync(
            request, binding!, authority.Actor, now, ct);
        if (!decision.IsAllowed || !HasValidGrants(binding!, decision, authority.Actor, now))
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        var snapshot = new ResourceAuthorizationSnapshot(
            request, binding!, authority, decision.PrincipalTenantId, decision.Grants);
        authorizationContext.RegisterValidator(token => evaluator.ValidateForCommitAsync(snapshot, clock.GetUtcNow(), token));
        return AuthorizationDecision.Allowed;
    }

    private static bool HasValidGrants(
        ResourcePolicyBinding binding,
        ResourceAuthorizationDecision decision,
        MembershipSnapshot actor,
        DateTimeOffset now)
    {
        var principalPolicy = binding.Policy is "venue_principal" or "artist_principal"
            or "either_principal" or "principal_administration";
        if (principalPolicy && decision.PrincipalTenantId != actor.TenantId)
            return false;

        if (decision.Grants.IsDefault
            || decision.Grants.Select(grant => grant.GrantId).Distinct().Count() != decision.Grants.Length
            || decision.Grants.Any(grant => grant.GrantId == Guid.Empty
                || grant.Version <= 0
                || string.IsNullOrWhiteSpace(grant.Scope)
                || grant.ValidFrom > now
                || grant.ValidUntil <= now
                || grant.ValidUntil <= grant.ValidFrom))
            return false;

        if (binding.RequiredScopes.Any(scope =>
            decision.Grants.Count(grant => grant.Scope == scope) != 1))
            return false;

        return binding.Resource != ResourceKind.Conversation
            || binding.Policy != "principal_administration"
            || decision.Grants.Count(grant => grant.Scope == "Read") == 1;
    }
}
