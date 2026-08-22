
namespace Concertable.B2B.Artist.Infrastructure.Mappers;

internal static class QueryableArtistMappers
{
    extension(IQueryable<ArtistEntity> query)
    {
        public IQueryable<ArtistSummary> ToSummary(IQueryable<ArtistRatingProjection> ratings) =>
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

        public IQueryable<ArtistDetails> ToDetails(IQueryable<ArtistRatingProjection> ratings) =>
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

        public IQueryable<ArtistProfile> ToProfile() =>
            query.Select(artist => new ArtistProfile(
                artist.Id,
                artist.TenantId,
                artist.UserId,
                artist.Name,
                artist.About,
                artist.Email,
                artist.Genres.ToHashSet()));
    }
}
