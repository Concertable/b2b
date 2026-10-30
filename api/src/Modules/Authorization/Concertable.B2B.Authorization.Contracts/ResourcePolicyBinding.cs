using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourcePolicyBinding(
    TenantPermission Permission,
    ResourceKind Resource,
    ResourceFacet? Facet,
    ResourcePolicy Policy,
    ImmutableArray<string> RequiredScopes)
{
    public static ResourcePolicyBinding FromCatalog(
        TenantPermission permission,
        ResourceKind kind,
        ResourceFacet? facet)
    {
        var source = AuthorizationCatalog.Permissions[permission].ResourceBindings.Single(binding =>
            string.Equals(binding.Resource, kind.ToString(), StringComparison.OrdinalIgnoreCase)
            && binding.Facet == facet?.ToString());
        return new ResourcePolicyBinding(permission, kind, facet,
            source.Policy, source.RequiresScopes.ToImmutableArray());
    }

    public bool IsSatisfiedBy(ResourceAuthorizationDecision decision, MembershipSnapshot actor, DateTimeOffset now)
    {
        if (Policy.IsPrincipal && decision.PrincipalTenantId != actor.TenantId)
            return false;

        if (decision.Grants.IsDefault
            || decision.Grants.Select(grant => grant.GrantId).Distinct().Count() != decision.Grants.Length
            || !decision.Grants.All(grant => grant.IsLiveAt(now)))
            return false;

        if (RequiredScopes.Any(scope => decision.Grants.Count(grant => grant.Scope == scope) != 1))
            return false;

        return Resource != ResourceKind.Conversation
            || Policy != ResourcePolicy.PrincipalAdministration
            || decision.Grants.Count(grant => grant.Scope == nameof(ResourceFacet.Read)) == 1;
    }
}
