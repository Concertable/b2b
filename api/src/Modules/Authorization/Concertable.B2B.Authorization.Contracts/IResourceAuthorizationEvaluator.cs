namespace Concertable.B2B.Authorization.Contracts;

public interface IResourceAuthorizationEvaluator
{
    ResourceKind Kind { get; }

    Task<ResourceAuthorizationDecision> CheckAsync(
        AuthorizationRequest request,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<ResourceAuthorizationDecision> RequireAsync(
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
