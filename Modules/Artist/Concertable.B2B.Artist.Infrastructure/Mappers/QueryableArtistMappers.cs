using Concertable.B2B.Artist.Domain;

namespace Concertable.B2B.Artist.Infrastructure.Mappers;

internal static class QueryableArtistMappers
{
    public static IQueryable<ArtistSummaryDto> ToSummaryDto(
        this IQueryable<ArtistEntity> query,
        IQueryable<ArtistRatingProjection> ratings) =>
        from a in query
        join r in ratings on a.Id equals r.ArtistId into rg
        from rating in rg.DefaultIfEmpty()
        select new ArtistSummaryDto
        {
            Id = a.Id,
            Name = a.Name,
            Avatar = a.Avatar,
            Rating = rating == null ? 0.0 : rating.AverageRating,
            Genres = a.Genres
        };

    public static IQueryable<ArtistDto> ToDto(
        this IQueryable<ArtistEntity> query,
        IQueryable<ArtistRatingProjection> ratings) =>
        from a in query
        where a.Address != null
        join r in ratings on a.Id equals r.ArtistId into rg
        from rating in rg.DefaultIfEmpty()
        select new ArtistDto
        {
            Id = a.Id,
            Name = a.Name,
            About = a.About,
            BannerUrl = a.BannerUrl,
            Avatar = a.Avatar,
            County = a.Address!.County,
            Town = a.Address!.Town,
            Email = a.Email ?? string.Empty,
            Rating = rating == null ? 0.0 : rating.AverageRating,
            Genres = a.Genres
        };
}
