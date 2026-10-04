namespace Concertable.B2B.Authorization.Contracts;

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
