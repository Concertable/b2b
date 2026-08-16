using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class OpportunityDashboardMappers
{
    extension(IEnumerable<OpportunityApplicationProjection> projections)
    {
        public IReadOnlyList<OpportunityApplicationMetrics> ToApplicationMetrics(
            IReadOnlyDictionary<int, IDeal> deals,
            DateTime today) =>
            projections.Select(projection => new OpportunityApplicationMetrics(
                projection.ToDto(deals[projection.DealId]),
                projection.ApplicationCount,
                Math.Max(0, (projection.StartDate.Date.AddDays(-7) - today).Days)))
                .ToList();
    }

    extension(IEnumerable<OpportunityMatchProjection> projections)
    {
        public IReadOnlyList<OpportunityMatch> ToMatches(
            IReadOnlyDictionary<int, IDeal> deals,
            IReadOnlySet<Genre> artistGenres) =>
            projections.Select(projection => new OpportunityMatch(
                projection.ToDto(deals[projection.DealId]),
                projection.County,
                projection.Town,
                projection.Genres.CalculateFitScore(artistGenres)))
                .ToList();
    }

    extension(OpportunityApplicationProjection projection)
    {
        private OpportunityDto ToDto(IDeal deal) => new()
        {
            Id = projection.Id,
            VenueId = projection.VenueId,
            VenueName = projection.VenueName,
            DealId = projection.DealId,
            Deal = deal,
            StartDate = projection.StartDate,
            EndDate = projection.EndDate,
            Genres = projection.Genres
        };
    }

    extension(OpportunityMatchProjection projection)
    {
        private OpportunityDto ToDto(IDeal deal) => new()
        {
            Id = projection.Id,
            VenueId = projection.VenueId,
            VenueName = projection.VenueName,
            DealId = projection.DealId,
            Deal = deal,
            StartDate = projection.StartDate,
            EndDate = projection.EndDate,
            Genres = projection.Genres
        };
    }

    extension(IReadOnlyList<Genre> opportunityGenres)
    {
        private int CalculateFitScore(IReadOnlySet<Genre> artistGenres)
        {
            if (opportunityGenres.Count == 0)
                return 100;

            var matchingGenres = opportunityGenres.Count(artistGenres.Contains);
            return (int)Math.Round(matchingGenres * 100d / opportunityGenres.Count);
        }
    }
}
