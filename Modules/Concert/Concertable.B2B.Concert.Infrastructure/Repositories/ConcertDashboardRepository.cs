using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using Concertable.DataAccess.Application.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertDashboardRepository : IConcertDashboardRepository
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

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
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

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(int artistId, CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
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
