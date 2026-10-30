using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public readonly record struct ResourceAuthorizationDecision
{
    private ResourceAuthorizationDecision(Guid? principalTenantId, ImmutableArray<ResourceGrantSnapshot> grants)
    {
        this.IsAllowed = true;
        this.PrincipalTenantId = principalTenantId;
        this.Grants = grants;
    }

    public static ResourceAuthorizationDecision Denied => default;

    public bool IsAllowed { get; }

    public Guid? PrincipalTenantId { get; }

    public ImmutableArray<ResourceGrantSnapshot> Grants { get; }

    public static ResourceAuthorizationDecision Allow(
        Guid? principalTenantId, ImmutableArray<ResourceGrantSnapshot> grants) =>
        new(principalTenantId, grants);
}
