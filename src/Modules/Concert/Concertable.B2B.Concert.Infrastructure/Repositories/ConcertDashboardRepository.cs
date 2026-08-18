using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.State;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertDashboardRepository : IConcertDashboardRepository
{
    private readonly ConcertDbContext context;
    private readonly IUpcomingSpecification<ConcertEntity> concertUpcoming;
    private readonly IEndedSpecification ended;
    private readonly IDoorRevenueOutstandingSpecification doorRevenueOutstanding;

    public ConcertDashboardRepository(
        ConcertDbContext context,
        IUpcomingSpecification<ConcertEntity> concertUpcoming,
        IEndedSpecification ended,
        IDoorRevenueOutstandingSpecification doorRevenueOutstanding)
    {
        this.context = context;
        this.concertUpcoming = concertUpcoming;
        this.ended = ended;
        this.doorRevenueOutstanding = doorRevenueOutstanding;
    }

    public Task<VenueConcertDashboardCounts?> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.VenueTenantId == venueTenantId));

        var awaitingDoorRevenue = ended
            .And(doorRevenueOutstanding)
            .Apply(context.Concerts.Where(c =>
                c.VenueTenantId == venueTenantId &&
                (c.State == ConcertState.Draft || c.State == ConcertState.Posted)));

        return context.VenueReadModels
            .Where(v => v.TenantId == venueTenantId)
            .ToVenueCounts(upcomingConcerts, awaitingDoorRevenue)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ArtistConcertDashboardCounts?> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        var upcomingConcerts = concertUpcoming.Apply(
            context.Concerts.Where(c => c.ArtistTenantId == artistTenantId));

        return context.ArtistReadModels
            .Where(a => a.TenantId == artistTenantId)
            .ToArtistCounts(upcomingConcerts)
            .FirstOrDefaultAsync(ct);
    }
}
