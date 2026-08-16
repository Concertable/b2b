using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class OpportunityRepository : OpportunityRepository<ConcertDbContext>, IOpportunityRepository
{
    public OpportunityRepository(ConcertDbContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant, timeProvider) { }

    public override Task<OpportunityEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Opportunities
            .Include(o => o.Venue)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<OpportunityApplicationProjection>> GetOpenWithApplicationCountsByVenueIdAsync(
        int venueId) =>
        await ActiveForVenue(venueId)
            .AsNoTracking()
            .Take(5)
            .Select(o => new OpportunityApplicationProjection
            {
                Id = o.Id,
                VenueId = o.VenueId,
                VenueName = o.Venue.Name,
                StartDate = o.Period.Start,
                EndDate = o.Period.End,
                Genres = o.Genres,
                DealId = o.DealId,
                ApplicationCount = o.Applications.Count
            })
            .ToListAsync();

    public async Task<Guid?> GetOwnerByIdAsync(int opportunityId) =>
        await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (Guid?)o.Venue.UserId)
            .FirstOrDefaultAsync();

    public Task<int?> GetDealIdByIdAsync(int opportunityId) =>
        context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (int?)o.DealId)
            .FirstOrDefaultAsync();

    public Task<DateRange?> GetPeriodByIdAsync(int opportunityId) =>
        context.Opportunities
            .AsNoTracking()
            .Where(o => o.Id == opportunityId)
            .Select(o => (DateRange?)o.Period)
            .FirstOrDefaultAsync();

    public async Task<OpportunityEntity?> GetByApplicationIdAsync(int id) =>
        await context.Opportunities
            .Where(o => o.Applications.Any(a => a.Id == id))
            .FirstOrDefaultAsync();

    public async Task<(string Name, Guid UserId)?> GetVenueSummaryByIdAsync(int opportunityId)
    {
        var venue = await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => new { o.Venue.Name, o.Venue.UserId })
            .FirstOrDefaultAsync();
        return venue is null ? null : (venue.Name, venue.UserId);
    }
}
