using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceAuthorizationSnapshot(
    AuthorizationRequest Request,
    ResourcePolicyBinding Binding,
    AuthoritySnapshot Authority,
    Guid? PrincipalTenantId,
    ImmutableArray<ResourceGrantSnapshot> Grants);
