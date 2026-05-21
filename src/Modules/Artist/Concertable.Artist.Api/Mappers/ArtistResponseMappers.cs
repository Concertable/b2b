using Concertable.Artist.Api.Responses;

namespace Concertable.Artist.Api.Mappers;

public static class ArtistResponseMappers
{
    public static ArtistDetailsResponse ToDetailsResponse(this ArtistDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        About = dto.About,
        BannerUrl = dto.BannerUrl,
        Avatar = dto.Avatar,
        Rating = dto.Rating,
        Genres = dto.Genres.ToList(),
        County = dto.County,
        Town = dto.Town,
        Email = dto.Email
    };
}
