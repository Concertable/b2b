namespace Concertable.B2B.Venue.Contracts;

/// <summary>The venue as this module publishes it to in-process callers, projected from
/// <c>VenueEntity</c>. Carries <see cref="Email"/> and <see cref="UserId"/> for checkout payee
/// construction, so it must never be serialized verbatim into a response a counterparty tenant can see —
/// map to <see cref="VenueSummary"/> first.</summary>
public sealed record VenueDto(
    int Id,
    Guid TenantId,
    Guid UserId,
    string Name,
    string About,
    string BannerUrl,
    string Avatar,
    string Email,
    string County,
    string Town);
