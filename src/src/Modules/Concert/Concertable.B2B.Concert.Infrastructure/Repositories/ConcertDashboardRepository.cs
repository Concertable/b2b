using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertDashboardRepository : IConcertDashboardRepository
{
    private readonly TenantConcertDbContext context;
    private readonly IUpcomingSpecification<OpportunityEntity> opportunityUpcoming;
    private readonly IUpcomingSpecification<ConcertEntity> concertUpcoming;
    private readonly IEndedAndBookedSpecification endedAndBooked;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;

    public ConcertDashboardRepository(
        TenantConcertDbContext context,
        IUpcomingSpecification<OpportunityEntity> opportunityUpcoming,
        IUpcomingSpecification<ConcertEntity> concertUpcoming,
        IEndedAndBookedSpecification endedAndBooked,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding)
    {
        this.context = context;
        this.opportunityUpcoming = opportunityUpcoming;
        this.concertUpcoming = concertUpcoming;
        this.endedAndBooked = endedAndBooked;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
    }

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Applied && a.Opportunity.VenueId == venueId),
            a => a.Opportunity);

        var openOpportunities = opportunityUpcoming.Apply(
            context.Opportunities
                .Where(o => o.VenueId == venueId)
                .WhereOpen());

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.VenueId == venueId));

        var awaitingDoorRevenue = endedAndBooked
            .And(doorRevenueOutstanding)
            .Apply(context.Concerts.Where(c => c.VenueId == venueId));

        return context.VenueReadModels
            .Where(v => v.Id == venueId)
            .ToVenueCounts(applications, openOpportunities, upcomingConcerts, awaitingDoorRevenue)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(
        int artistId,
        IReadOnlyCollection<DealType> checkoutCapableDealTypes,
        CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Applied && a.ArtistId == artistId),
            a => a.Opportunity);

        var acceptedAwaitingCheckout = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Accepted
                    && a.ArtistId == artistId
                    && checkoutCapableDealTypes.Contains(a.DealType)),
            a => a.Opportunity);

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.ArtistId == artistId));

        return context.ArtistReadModels
            .Where(a => a.Id == artistId)
            .ToArtistCounts(applications, acceptedAwaitingCheckout, upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }
}
