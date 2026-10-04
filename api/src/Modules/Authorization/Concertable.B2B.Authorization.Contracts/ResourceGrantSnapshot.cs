namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceGrantSnapshot(
    string Scope,
    Guid GrantId,
    long Version,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil);
