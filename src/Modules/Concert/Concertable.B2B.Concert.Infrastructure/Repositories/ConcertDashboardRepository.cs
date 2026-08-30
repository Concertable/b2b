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
    private readonly IUpcomingSpecification<OpportunityEntity> opportunityUpcomingSpec;
    private readonly IUpcomingSpecification<ConcertEntity> concertUpcomingSpec;
    private readonly IEndedAndBookedSpecification endedAndBookedSpec;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstandingSpec;

    public ConcertDashboardRepository(
        ConcertDbContext context,
        IUpcomingSpecification<OpportunityEntity> opportunityUpcomingSpec,
        IUpcomingSpecification<ConcertEntity> concertUpcomingSpec,
        IEndedAndBookedSpecification endedAndBookedSpec,
        IDoorRevenueOutstandingSpecification doorRevenueOutstandingSpec)
    {
        this.context = context;
        this.opportunityUpcomingSpec = opportunityUpcomingSpec;
        this.concertUpcomingSpec = concertUpcomingSpec;
        this.endedAndBookedSpec = endedAndBookedSpec;
        this.doorRevenueOutstandingSpec = doorRevenueOutstandingSpec;
    }

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var applications = context.Applications
                .Where(a => a.State == LifecycleState.Applied
                    && a.VenueTenantId == venueTenantId)
                .Where(opportunityUpcomingSpec.ToExpression(a => a.Opportunity));

        var openOpportunities = context.Opportunities
                .Where(o => o.TenantId == venueTenantId)
                .WhereOpen()
                .Where(opportunityUpcomingSpec.ToExpression());

        var upcomingConcerts = context.Concerts
            .Where(c => c.VenueTenantId == venueTenantId)
            .Where(concertUpcomingSpec.ToExpression());

        var awaitingDoorRevenuePredicate = endedAndBookedSpec
            .And(doorRevenueOutstandingSpec)
            .ToExpression();

        var awaitingDoorRevenueConcerts = context.Concerts
            .Where(c => c.VenueTenantId == venueTenantId)
            .Where(awaitingDoorRevenuePredicate);

        return context.VenueReadModels
            .Where(v => v.TenantId == venueTenantId)
            .ToVenueCounts(applications, openOpportunities, upcomingConcerts, awaitingDoorRevenueConcerts)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(
        Guid artistTenantId,
        IReadOnlyCollection<DealType> checkoutCapableDealTypes,
        CancellationToken ct = default)
    {
        var applications = context.Applications
                .Where(a => a.State == LifecycleState.Applied
                    && a.ArtistTenantId == artistTenantId)
                .Where(opportunityUpcomingSpec.ToExpression(a => a.Opportunity));

        var acceptedAwaitingCheckout = context.Applications
                .Where(a => a.State == LifecycleState.Accepted
                    && a.ArtistTenantId == artistTenantId
                    && checkoutCapableDealTypes.Contains(a.DealType))
                .Where(opportunityUpcomingSpec.ToExpression(a => a.Opportunity));

        var upcomingConcerts = context.Concerts
            .Where(c => c.ArtistTenantId == artistTenantId)
            .Where(concertUpcomingSpec.ToExpression());

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
