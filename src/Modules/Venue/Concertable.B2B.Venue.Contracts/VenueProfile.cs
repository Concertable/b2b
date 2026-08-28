namespace Concertable.B2B.Venue.Contracts;

/// <summary>A venue's full cross-module profile, including <see cref="Email"/> and <see cref="UserId"/> for
/// in-process consumers such as checkout payee construction. Never serialize this verbatim into an HTTP
/// response visible to a counterparty tenant — map to a display-only shape first.</summary>
public sealed record VenueProfile(
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
