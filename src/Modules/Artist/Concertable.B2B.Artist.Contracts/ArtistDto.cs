using Concertable.Contracts.Enums;

namespace Concertable.B2B.Artist.Contracts;

/// <summary>The artist as this module publishes it to in-process callers, projected from
/// <c>ArtistEntity</c>. Carries <see cref="Email"/> for checkout payee construction, so it must never be
/// serialized verbatim into a response a counterparty tenant can see — map to <see cref="ArtistSummary"/>
/// first.</summary>
public sealed record ArtistDto(
    int Id,
    Guid TenantId,
    string Name,
    string About,
    string BannerUrl,
    string Avatar,
    string Email,
    IReadOnlySet<Genre> Genres);
