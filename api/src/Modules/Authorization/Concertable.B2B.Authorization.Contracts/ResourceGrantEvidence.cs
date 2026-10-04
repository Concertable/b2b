namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceGrantEvidence(
    string Scope,
    Guid GrantId,
    long Version,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil);
