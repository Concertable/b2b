namespace Concertable.B2B.Authorization.Contracts;

public sealed record ResourceGrantSnapshot(
    string Scope,
    Guid GrantId,
    long Version,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil)
{
    public bool IsLiveAt(DateTimeOffset now) =>
        GrantId != Guid.Empty
        && Version > 0
        && !string.IsNullOrWhiteSpace(Scope)
        && ValidFrom <= now
        && (ValidUntil is not { } until || until > now);
}
