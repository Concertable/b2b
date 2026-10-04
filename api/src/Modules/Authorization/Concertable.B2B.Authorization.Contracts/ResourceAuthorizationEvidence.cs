using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceAuthorizationEvidence(
    Guid? PrincipalTenantId,
    ImmutableArray<ResourceGrantSnapshot> Grants);
