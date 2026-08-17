using Concertable.Contracts.Enums;

namespace Concertable.B2B.Artist.Contracts;

public sealed record ArtistProfile(
    int Id,
    Guid TenantId,
    Guid UserId,
    string Name,
    string About,
    string Email,
    IReadOnlySet<Genre> Genres);
