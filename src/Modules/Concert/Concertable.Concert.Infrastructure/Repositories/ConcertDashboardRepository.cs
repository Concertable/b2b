using Concertable.DataAccess.Specifications;
using Concertable.Concert.Contracts;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Repositories;

internal class ConcertDashboardRepository : IConcertDashboardRepository
{
    private readonly ConcertDbContext context;
    private readonly IUpcomingSpecification<OpportunityEntity> opportunityUpcoming;
    private readonly IUpcomingSpecification<ConcertEntity> concertUpcoming;

    public ConcertDashboardRepository(
        ConcertDbContext context,
        IUpcomingSpecification<OpportunityEntity> opportunityUpcoming,
        IUpcomingSpecification<ConcertEntity> concertUpcoming)
    {
        this.context = context;
        this.opportunityUpcoming = opportunityUpcoming;
        this.concertUpcoming = concertUpcoming;
    }

    public Task<VenueDashboardCountsDto?> GetVenueCountsAsync(int venueId, CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyExpression(
            context.Applications
                .Where(a => a.Status == ApplicationStatus.Pending && a.Opportunity.VenueId == venueId),
            a => a.Opportunity);

        var openOpportunities = opportunityUpcoming.Apply(
            context.Opportunities
                .Where(o => o.VenueId == venueId)
                .Where(o => !o.Applications.Any(app => app.Status == ApplicationStatus.Accepted)));

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.VenueId == venueId));

        return context.VenueReadModels
            .Where(v => v.Id == venueId)
            .ToVenueCounts(applications, openOpportunities, upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ArtistDashboardCountsDto?> GetArtistCountsAsync(int artistId, CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyExpression(
            context.Applications
                .Where(a => a.Status == ApplicationStatus.Pending && a.ArtistId == artistId),
            a => a.Opportunity);

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.ArtistId == artistId));

        return context.ArtistReadModels
            .Where(a => a.Id == artistId)
            .ToArtistCounts(applications, upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }
}
