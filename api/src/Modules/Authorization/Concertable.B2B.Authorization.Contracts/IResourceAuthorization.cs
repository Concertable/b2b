using System.Collections.Immutable;

namespace Concertable.B2B.Authorization.Contracts;

public interface IResourceAuthorization
{
    Task<AuthorizationDecision> CheckAsync(AuthorizationRequest request, CancellationToken ct = default);

    Task<AuthorizationDecision> RequireAsync(AuthorizationRequest request, CancellationToken ct = default);
}

public sealed record ResourcePolicyBinding(
    TenantPermission Permission,
    ResourceKind Resource,
    ResourceFacet? Facet,
    string Policy,
    ImmutableArray<string> RequiredScopes);

public sealed record ResourceGrantEvidence(
    string Scope,
    Guid GrantId,
    long Version,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil);

public sealed record ResourceAuthorizationEvidence(
    Guid? PrincipalTenantId,
    ImmutableArray<ResourceGrantEvidence> Grants);

public sealed record ResourceAuthorizationProof(
    AuthorizationRequest Request,
    ResourcePolicyBinding Binding,
    AuthoritySnapshot Authority,
    ResourceAuthorizationEvidence Evidence);

public interface IResourceAuthorizationEvaluator
{
    ResourceKind Kind { get; }

    Task<ResourceAuthorizationEvidence?> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<ResourceAuthorizationEvidence?> RequireAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<bool> ValidateForCommitAsync(
        ResourceAuthorizationProof proof,
        DateTimeOffset now,
        CancellationToken ct = default);
}
