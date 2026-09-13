using Concertable.B2B.Artist.Api.Responses;

namespace Concertable.B2B.Artist.Api.Mappers;

internal static class ArtistMappers
{
    extension(ArtistDetails dto)
    {
        public DetailsResponse ToDetailsResponse() => new()
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
}
