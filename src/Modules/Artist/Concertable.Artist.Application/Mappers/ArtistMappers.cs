using Concertable.Artist.Application.DTOs;

namespace Concertable.Artist.Application.Mappers;

public static class ArtistMappers
{
    public static ArtistDto ToDto(this ArtistEntity artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        About = artist.About,
        BannerUrl = artist.BannerUrl,
        Avatar = artist.Avatar,
        Genres = artist.Genres,
        County = artist.Address?.County ?? string.Empty,
        Town = artist.Address?.Town ?? string.Empty,
        Email = artist.Email ?? string.Empty
    };

    public static ArtistSummaryDto ToSummaryDto(this ArtistEntity artist) => new()
    {
        Id = artist.Id,
        Name = artist.Name,
        Avatar = artist.Avatar,
        Genres = artist.Genres,
    };

    public static IEnumerable<ArtistDto> ToDtos(this IEnumerable<ArtistEntity> artists) =>
        artists.Select(a => a.ToDto());
}
