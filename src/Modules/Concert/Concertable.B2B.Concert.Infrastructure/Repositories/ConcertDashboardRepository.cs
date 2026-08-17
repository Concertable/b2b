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
    private readonly IEndedSpecification ended;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;

    public ConcertDashboardRepository(
        ConcertDbContext context,
        IUpcomingSpecification<OpportunityEntity> opportunityUpcoming,
        IUpcomingSpecification<ConcertEntity> concertUpcoming,
        IEndedSpecification ended,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding)
    {
        this.context = context;
        this.opportunityUpcoming = opportunityUpcoming;
        this.concertUpcoming = concertUpcoming;
        this.ended = ended;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
    }

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default)
    {
        var upcomingOpportunityIds = opportunityUpcoming.Apply(context.Opportunities)
            .Where(o => o.VenueId == venueId)
            .Select(o => o.Id);
        var applications = context.Applications.Where(a =>
            a.State == LifecycleState.Applied && upcomingOpportunityIds.Contains(a.OpportunityId));

        var openOpportunities = opportunityUpcoming.Apply(
            context.Opportunities
                .Where(o => o.VenueId == venueId)
                .WhereOpen(context.Applications));

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.VenueId == venueId));

        var awaitingDoorRevenue = ended
            .And(doorRevenueOutstanding)
            .Apply(context.Concerts.Where(c => c.VenueId == venueId && context.Applications.Any(a =>
                a.Id == c.ApplicationId && a.State == LifecycleState.Booked)));

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
        var upcomingOpportunityIds = opportunityUpcoming.Apply(context.Opportunities).Select(o => o.Id);
        var applications = context.Applications.Where(a =>
            a.State == LifecycleState.Applied
            && a.ArtistId == artistId
            && upcomingOpportunityIds.Contains(a.OpportunityId));

        var acceptedAwaitingCheckout = context.Applications.Where(a =>
            a.State == LifecycleState.Accepted
            && a.ArtistId == artistId
            && checkoutCapableDealTypes.Contains(a.DealType)
            && upcomingOpportunityIds.Contains(a.OpportunityId));

        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.ArtistId == artistId));

        return context.ArtistReadModels
            .Where(a => a.Id == artistId)
            .ToArtistCounts(applications, acceptedAwaitingCheckout, upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }
}
