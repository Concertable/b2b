namespace Concertable.B2B.Authorization.Contracts;

public sealed record AuthorizationRequest
{
    public TenantPermission Permission { get; }
    public ResourceAddress Resource { get; }
    public ResourceFacet? Facet { get; }

    public AuthorizationRequest(TenantPermission permission, ResourceAddress resource, ResourceFacet? facet = null)
    {
        if (!TenantPermission.All.Contains(permission))
            throw new ArgumentOutOfRangeException(nameof(permission));
        ArgumentNullException.ThrowIfNull(resource);
        if (facet.HasValue && !Enum.IsDefined(facet.Value))
            throw new ArgumentOutOfRangeException(nameof(facet));

        Permission = permission;
        Resource = resource;
        Facet = facet;
    }
}
