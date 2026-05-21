using Concertable.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Repositories;

internal class ApplicationRepository : Repository<ApplicationEntity>, IApplicationRepository
{
    private readonly TimeProvider timeProvider;

    public ApplicationRepository(ConcertDbContext context, TimeProvider timeProvider) : base(context)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<IEnumerable<ApplicationEntity>> GetByOpportunityIdAsync(int id)
    {
        return await context.Applications
            .Where(ca => ca.OpportunityId == id)
            .Include(ca => ca.Artist)
                .ThenInclude(a => a.Genres)
                    .ThenInclude(g => g.Genre)
            .Include(ca => ca.Opportunity)
                .ThenInclude(o => o.OpportunityGenres)
                    .ThenInclude(og => og.Genre)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationEntity>> GetPendingByArtistIdAsync(int artistId)
    {
        return await context.Applications
            .Include(a => a.Opportunity)
                .ThenInclude(o => o.Venue)
            .Where(a =>
                a.ArtistId == artistId &&
                !context.Bookings.Any(b => b.ApplicationId == a.Id) &&
                a.Opportunity.Period.Start > timeProvider.GetUtcNow())
            .ToListAsync();
    }

    public async Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id)
    {
        var query = await context.Applications
            .Where(ca => ca.Id == id)
            .Include(ca => ca.Artist)
            .Include(ca => ca.Opportunity)
                .ThenInclude(o => o.Venue)
            .FirstOrDefaultAsync();

        if (query is null) return null;
        return (query.Artist, query.Opportunity.Venue);
    }

    public async new Task<ApplicationEntity?> GetByIdAsync(int id)
    {
        return await context.Applications
            .Where(ca => ca.Id == id)
            .Include(ca => ca.Artist)
                .ThenInclude(a => a.Genres)
                    .ThenInclude(g => g.Genre)
            .Include(ca => ca.Opportunity)
                .ThenInclude(o => o.OpportunityGenres)
                    .ThenInclude(og => og.Genre)
            .FirstOrDefaultAsync();
    }

    public async Task RejectAllExceptAsync(int opportunityId, int applicationId)
    {
        await context.Applications
            .Where(a => a.OpportunityId == opportunityId && a.Id != applicationId && a.Status == ApplicationStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, ApplicationStatus.Rejected));
    }

    public Task<int?> GetContractIdByIdAsync(int applicationId)
    {
        return context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => (int?)a.Opportunity.ContractId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ApplicationEntity>> GetRecentDeniedByArtistIdAsync(int artistId)
    {
        return await context.Applications
            .Include(a => a.Opportunity)
                .ThenInclude(o => o.Venue)
            .Where(a =>
                a.ArtistId == artistId &&
                context.Bookings.Any(b =>
                    b.Application.OpportunityId == a.OpportunityId &&
                    b.ApplicationId != a.Id))
            .OrderByDescending(a => a.Opportunity.Period.End)
            .Take(5)
            .ToListAsync();
    }

}
