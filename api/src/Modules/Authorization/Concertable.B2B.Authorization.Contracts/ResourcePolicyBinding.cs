using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourcePolicyBinding(
    TenantPermission Permission,
    ResourceKind Resource,
    ResourceFacet? Facet,
    string Policy,
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
}
