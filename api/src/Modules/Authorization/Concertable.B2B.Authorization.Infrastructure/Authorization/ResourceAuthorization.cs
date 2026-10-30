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
        return decision.IsAllowed && binding!.IsSatisfiedBy(decision, authority.Actor, now)
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
        if (!decision.IsAllowed || !binding!.IsSatisfiedBy(decision, authority.Actor, now))
            return actor.Fail(AuthorizationDecision.Denied).Decision;

        var snapshot = new ResourceAuthorizationSnapshot(
            request, binding!, authority, decision.PrincipalTenantId, decision.Grants);
        authorizationContext.RegisterValidator(token => evaluator.ValidateForCommitAsync(snapshot, clock.GetUtcNow(), token));
        return AuthorizationDecision.Allowed;
    }
}
