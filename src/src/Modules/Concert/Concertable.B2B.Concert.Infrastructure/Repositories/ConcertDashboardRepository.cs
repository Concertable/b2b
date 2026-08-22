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
    private readonly ConcertDbContext context;
    private readonly IUpcomingSpecification<OpportunityEntity> opportunityUpcoming;
    private readonly IUpcomingSpecification<ConcertEntity> concertUpcoming;
    private readonly IEndedAndBookedSpecification endedAndBooked;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;

    public ConcertDashboardRepository(
        ConcertDbContext context,
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

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Applied
                    && a.VenueTenantId == venueTenantId),
            a => a.Opportunity);

        var openOpportunities = opportunityUpcoming.Apply(
            context.Opportunities
                .Where(o => o.TenantId == venueTenantId)
                .WhereOpen());

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.VenueTenantId == venueTenantId));

        var awaitingDoorRevenue = endedAndBooked
            .And(doorRevenueOutstanding)
            .Apply(context.Concerts.Where(c => c.VenueTenantId == venueTenantId));

        return context.VenueReadModels
            .Where(v => v.TenantId == venueTenantId)
            .ToVenueCounts(applications, openOpportunities, upcomingConcerts, awaitingDoorRevenue)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(
        Guid artistTenantId,
        IReadOnlyCollection<DealType> checkoutCapableDealTypes,
        CancellationToken ct = default)
    {
        var applications = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Applied
                    && a.ArtistTenantId == artistTenantId),
            a => a.Opportunity);

        var acceptedAwaitingCheckout = opportunityUpcoming.ApplyVia(
            context.Applications
                .Where(a => a.State == LifecycleState.Accepted
                    && a.ArtistTenantId == artistTenantId
                    && checkoutCapableDealTypes.Contains(a.DealType)),
            a => a.Opportunity);

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.ArtistTenantId == artistTenantId));

        return context.ArtistReadModels
            .Where(a => a.TenantId == artistTenantId)
            .ToArtistCounts(applications, acceptedAwaitingCheckout, upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default)
    {
        if (bookingIds.Count == 0)
            return [];

        return await context.Bookings
            .AsNoTracking()
            .Where(b => bookingIds.Contains(b.Id) && b.Concert != null)
            .Select(b => new ManagerSettlementContext(
                b.Id,
                b.Concert!.Id,
                b.Concert.Name,
                b.VenueTenantId,
                b.ArtistTenantId,
                b.Application.Opportunity.Venue.Name,
                b.Application.Artist.Name))
            .ToListAsync(ct);
    }
}
