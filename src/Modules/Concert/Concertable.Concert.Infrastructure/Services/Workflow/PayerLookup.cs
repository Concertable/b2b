using Concertable.Concert.Application.Responses;
using Concertable.Concert.Infrastructure.Data;
using Concertable.User.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Services.Workflow;

internal class PayerLookup : IPayerLookup
{
    private readonly ConcertDbContext context;
    private readonly IUserModule userModule;

    public PayerLookup(ConcertDbContext context, IUserModule userModule)
    {
        this.context = context;
        this.userModule = userModule;
    }

    public Task<Guid?> GetVenueManagerIdAsync(int applicationId) =>
        context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.Opportunity.Venue.UserId)
            .FirstOrDefaultAsync();

    public Task<Guid?> GetArtistManagerIdAsync(int applicationId) =>
        context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.Artist.UserId)
            .FirstOrDefaultAsync();

    public async Task<(Guid VenueManagerId, Guid ArtistManagerId)?> GetManagerIdsAsync(int applicationId)
    {
        var ids = await context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new
            {
                VenueManagerId = a.Opportunity.Venue.UserId,
                ArtistManagerId = a.Artist.UserId
            })
            .FirstOrDefaultAsync();

        return ids is null ? null : (ids.VenueManagerId, ids.ArtistManagerId);
    }

    public Task<PayeeSummary?> GetArtistAsync(int applicationId) =>
        context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new PayeeSummary(a.Artist.Name, a.Artist.Email))
            .FirstOrDefaultAsync()!;

    public async Task<PayeeSummary?> GetVenueAsync(int applicationId)
    {
        var venue = await context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.Opportunity.Venue.Name, a.Opportunity.Venue.UserId })
            .FirstOrDefaultAsync();

        if (venue is null) return null;

        var manager = await userModule.GetManagerByIdAsync(venue.UserId);
        return new PayeeSummary(venue.Name, manager?.Email);
    }

    public async Task<PayeeSummary?> GetVenueByOpportunityIdAsync(int opportunityId)
    {
        var venue = await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => new { o.Venue.Name, o.Venue.UserId })
            .FirstOrDefaultAsync();

        if (venue is null) return null;

        var manager = await userModule.GetManagerByIdAsync(venue.UserId);
        return new PayeeSummary(venue.Name, manager?.Email);
    }
}
