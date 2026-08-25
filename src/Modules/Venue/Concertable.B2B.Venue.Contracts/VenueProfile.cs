namespace Concertable.B2B.Venue.Contracts;

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
