namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceAuthorizationProof(
    AuthorizationRequest Request,
    ResourcePolicyBinding Binding,
    AuthoritySnapshot Authority,
    ResourceAuthorizationEvidence Evidence);
