using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class ResourceAuthorization(
    ActorAuthoritySession actor,
    ICommandAuthorizationContext command,
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
        var evidence = await registry.Evaluator(request.Resource.Kind).CheckAsync(
            request, binding!, authority.Actor, now, ct);
        return evidence is null || !HasValidEvidence(binding!, evidence, authority.Actor, now)
            ? AuthorizationDecision.Denied
            : AuthorizationDecision.Allowed;
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
        var evidence = await evaluator.RequireAsync(
            request, binding!, authority.Actor, now, ct);
        if (evidence is null || !HasValidEvidence(binding!, evidence, authority.Actor, now))
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        var proof = new ResourceAuthorizationProof(request, binding!, authority, evidence);
        command.RegisterValidator(token => evaluator.ValidateForCommitAsync(proof, clock.GetUtcNow(), token));
        return AuthorizationDecision.Allowed;
    }

    private static bool HasValidEvidence(
        ResourcePolicyBinding binding,
        ResourceAuthorizationEvidence evidence,
        MembershipSnapshot actor,
        DateTimeOffset now)
    {
        var principalPolicy = binding.Policy is "venue_principal" or "artist_principal"
            or "either_principal" or "principal_administration";
        if (principalPolicy && evidence.PrincipalTenantId != actor.TenantId)
            return false;

        if (evidence.Grants.IsDefault
            || evidence.Grants.Select(grant => grant.GrantId).Distinct().Count() != evidence.Grants.Length
            || evidence.Grants.Any(grant => grant.GrantId == Guid.Empty
                || grant.Version <= 0
                || string.IsNullOrWhiteSpace(grant.Scope)
                || grant.ValidFrom > now
                || grant.ValidUntil <= now
                || grant.ValidUntil <= grant.ValidFrom))
            return false;

        if (binding.RequiredScopes.Any(scope =>
            evidence.Grants.Count(grant => grant.Scope == scope) != 1))
            return false;

        return binding.Resource != ResourceKind.Conversation
            || binding.Policy != "principal_administration"
            || evidence.Grants.Count(grant => grant.Scope == "Read") == 1;
    }
}
