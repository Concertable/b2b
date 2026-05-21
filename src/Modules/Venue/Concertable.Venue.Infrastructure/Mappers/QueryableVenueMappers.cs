using Concertable.Venue.Application.DTOs;
using Concertable.Venue.Domain;

namespace Concertable.Venue.Infrastructure.Mappers;

internal static class QueryableVenueMappers
{
    public static IQueryable<VenueSummaryDto> ToSummaryDto(
        this IQueryable<VenueEntity> query,
        IQueryable<VenueRatingProjection> ratings) =>
        from v in query.Where(v => v.Location != null && v.Address != null)
        join r in ratings on v.Id equals r.VenueId into rg
        from rating in rg.DefaultIfEmpty()
        select new VenueSummaryDto
        {
            Id = v.Id,
            Name = v.Name,
            Avatar = v.Avatar,
            Rating = rating == null ? 0.0 : rating.AverageRating
        };

    public static IQueryable<VenueDto> ToDto(
        this IQueryable<VenueEntity> query,
        IQueryable<VenueRatingProjection> ratings) =>
        from v in query.Where(v => v.Location != null && v.Address != null)
        join r in ratings on v.Id equals r.VenueId into rg
        from rating in rg.DefaultIfEmpty()
        select new VenueDto
        {
            Id = v.Id,
            Name = v.Name,
            About = v.About,
            BannerUrl = v.BannerUrl,
            Avatar = v.Avatar,
            Approved = v.Approved,
            County = v.Address!.County,
            Town = v.Address!.Town,
            Email = v.Email ?? string.Empty,
            Latitude = v.Location!.Y,
            Longitude = v.Location!.X,
            Rating = rating == null ? 0.0 : rating.AverageRating
        };
}
