using Concertable.B2B.Artist.Domain;

namespace Concertable.B2B.Artist.Infrastructure.Mappers;

internal static class QueryableArtistMappers
{
    public static IQueryable<ArtistSummary> ToSummary(
        this IQueryable<ArtistEntity> query,
        IQueryable<ArtistRatingProjection> ratings) =>
        from a in query
        join r in ratings on a.Id equals r.ArtistId into rg
        from rating in rg.DefaultIfEmpty()
        select new ArtistSummary
        {
            Id = a.Id,
            Name = a.Name,
            Avatar = a.Avatar,
            Rating = rating == null ? 0.0 : rating.AverageRating,
            Genres = a.Genres
        };

    public static IQueryable<ArtistDetails> ToDetails(
        this IQueryable<ArtistEntity> query,
        IQueryable<ArtistRatingProjection> ratings) =>
        from a in query
        join r in ratings on a.Id equals r.ArtistId into rg
        from rating in rg.DefaultIfEmpty()
        select new ArtistDetails
        {
            Id = a.Id,
            Name = a.Name,
            About = a.About,
            BannerUrl = a.BannerUrl,
            Avatar = a.Avatar,
            County = a.Address.County,
            Town = a.Address.Town,
            Email = a.Email,
            Rating = rating == null ? 0.0 : rating.AverageRating,
            Genres = a.Genres
        };
}
