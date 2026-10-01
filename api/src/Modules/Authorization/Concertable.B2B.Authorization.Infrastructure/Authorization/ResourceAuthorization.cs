using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class ResourceAuthorization(
    IMembershipContext membership,
    IAuthorityResolver authorityResolver,
    ICommandAuthorizationContext command,
    ResourceBindingRegistry registry,
    TimeProvider clock) : IResourceAuthorization
{
    private AuthoritySnapshot? retainedAuthority;
    private Guid? retainedTransactionId;

    public async Task<AuthorizationDecision> CheckAsync(AuthorizationRequest request, CancellationToken ct = default)
    {
        if (membership.Membership is not { } requestActor
            || !registry.TryResolve(request, out var binding))
            return AuthorizationDecision.Denied;

        var option = await authorityResolver.ResolveAsync(requestActor, ct);
        if (!option.TryGetValue(out var authority)
            || !authority.Actor.HasSameAuthorityAs(requestActor)
            || authority.CatalogRevision != AuthorizationCatalog.Revision)
            return AuthorizationDecision.AuthorityChanged;

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
        if (!command.IsActive)
            throw new InvalidOperationException("Resource authorization requires an active command.");

        if (retainedTransactionId != command.TransactionId)
        {
            retainedAuthority = null;
            retainedTransactionId = command.TransactionId;
        }

        if (membership.Membership is not { } requestActor
            || !registry.TryResolve(request, out var binding))
            return Fail(AuthorizationDecision.Denied);

        var authority = retainedAuthority;
        if (authority is null)
        {
            var option = await authorityResolver.ResolveForCommandAsync(requestActor, ct);
            if (!option.TryGetValue(out authority)
                || !authority.Actor.HasSameAuthorityAs(requestActor)
                || authority.CatalogRevision != AuthorizationCatalog.Revision)
                return Fail(AuthorizationDecision.AuthorityChanged);

            retainedAuthority = authority;
            command.RegisterValidator(token => authorityResolver.ValidateForCommitAsync(authority, token));
        }
        else if (authority.Actor.MembershipId != requestActor.MembershipId
                 || authority.Actor.TenantId != requestActor.TenantId
                 || authority.Actor.UserId != requestActor.UserId)
            return Fail(AuthorizationDecision.AuthorityChanged);

        if (!authority.Actor.HasPermission(request.Permission))
            return Fail(AuthorizationDecision.Denied);

        var evaluator = registry.Evaluator(request.Resource.Kind);
        var now = clock.GetUtcNow();
        var evidence = await evaluator.RequireAsync(
            request, binding!, authority.Actor, now, ct);
        if (evidence is null || !HasValidEvidence(binding!, evidence, authority.Actor, now))
            return Fail(AuthorizationDecision.Denied);

        var proof = new ResourceAuthorizationProof(request, binding!, authority, evidence);
        command.RegisterValidator(token => evaluator.ValidateForCommitAsync(proof, clock.GetUtcNow(), token));
        return AuthorizationDecision.Allowed;
    }

    private AuthorizationDecision Fail(AuthorizationDecision decision)
    {
        command.MarkAuthorityFailed();
        return decision;
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
