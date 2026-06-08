using Concertable.B2B.Artist.Application.DTOs;

namespace Concertable.B2B.Artist.Application.Mappers;

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
        County = artist.Address.County,
        Town = artist.Address.Town,
        Email = artist.Email
    };
}
