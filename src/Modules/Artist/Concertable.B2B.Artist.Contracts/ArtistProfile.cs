using Concertable.Contracts.Enums;

namespace Concertable.B2B.Artist.Contracts;

/// <summary>An artist's full cross-module profile, including <see cref="Email"/> and <see cref="UserId"/> for
/// in-process consumers such as checkout payee construction. Never serialize this verbatim into an HTTP
/// response visible to a counterparty tenant — map to a display-only shape first.</summary>
public sealed record ArtistProfile(
    int Id,
    Guid TenantId,
    Guid UserId,
    string Name,
    string About,
    string BannerUrl,
    string Avatar,
    string Email,
    IReadOnlySet<Genre> Genres);
