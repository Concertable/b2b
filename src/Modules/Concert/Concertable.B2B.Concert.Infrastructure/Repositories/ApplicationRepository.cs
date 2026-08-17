using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ApplicationRepository : VenueArtistTenantScopedRepository<ApplicationEntity>, IApplicationRepository
{
    private readonly TimeProvider timeProvider;

    public ApplicationRepository(ConcertDbContext context, TimeProvider timeProvider) : base(context)
    {
        this.timeProvider = timeProvider;
    }

    public Task<FinancialOperation?> GetFinancialOperationAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Applications
            .Where(application =>
                application.Id == applicationId &&
                (application.CancellationOperationId != null || application.AcceptanceOperationId != null))
            .Select(application => new FinancialOperation(
                application.CancellationOperationId ?? application.AcceptanceOperationId ?? Guid.Empty,
                application.State,
                application.FinancialFailureCode,
                application.FinancialFailureMessage))
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<ApplicationEntity>> GetByOpportunityIdAsync(int id)
    {
        return await context.Applications
            .Where(ca => ca.OpportunityId == id)
            .Include(ca => ca.Artist)
                .ThenInclude(a => a.Genres)
            .ToListAsync();
    }

    public Task<bool> ExistsForOpportunityAndArtistAsync(int opportunityId, int artistId) =>
        context.Applications.AnyAsync(a => a.OpportunityId == opportunityId && a.ArtistId == artistId);

    public async Task<IEnumerable<ApplicationEntity>> GetPendingByArtistIdAsync(int artistId)
    {
        return await context.Applications
            .Include(a => a.Artist)
                .ThenInclude(a => a.Genres)
            .Where(a =>
                a.ArtistId == artistId &&
                !context.Bookings.Any(b => b.ApplicationId == a.Id) &&
                context.Opportunities.Any(o => o.Id == a.OpportunityId && o.Period.Start > timeProvider.GetUtcNow()))
            .ToListAsync();
    }

    public async Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id)
    {
        var query = await (
            from application in context.Applications
            join opportunity in context.Opportunities on application.OpportunityId equals opportunity.Id
            join venue in context.VenueReadModels on opportunity.VenueId equals venue.Id
            where application.Id == id
            select new { application.Artist, Venue = venue })
            .FirstOrDefaultAsync();

        if (query is null) return null;
        return (query.Artist, query.Venue);
    }

    public async Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(int applicationId)
    {
        var row = await context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.VenueTenantId, a.ArtistTenantId })
            .FirstOrDefaultAsync();

        return row is null ? null : (row.VenueTenantId, row.ArtistTenantId);
    }

    public async Task<(LifecycleState State, PaymentVerification Verification)?> GetLifecycleAndPaymentStateAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var row = await context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.State, a.PaymentVerification })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : (row.State, row.PaymentVerification);
    }

    public override async Task<ApplicationEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Applications
            .Where(ca => ca.Id == id)
            .Include(ca => ca.Artist)
                .ThenInclude(a => a.Genres)
            .FirstOrDefaultAsync(ct);
    }

    public async Task RejectAllExceptAsync(int opportunityId, int applicationId)
    {
        await context.Applications
            .Where(a => a.OpportunityId == opportunityId && a.Id != applicationId && a.State == LifecycleState.Applied)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.State, LifecycleState.Rejected));
    }

    public Task<int?> GetDealIdByIdAsync(int applicationId)
    {
        return context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => context.Opportunities
                .Where(o => o.Id == a.OpportunityId)
                .Select(o => (int?)o.DealId)
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }

    public Task<PayeeSummary?> GetArtistPayeeAsync(int applicationId)
    {
        return context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new PayeeSummary(a.Artist.Name, a.Artist.Email))
            .FirstOrDefaultAsync()!;
    }

    public Task<Guid?> GetVenueManagerIdAsync(int applicationId)
    {
        return context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => context.Opportunities
                .Where(o => o.Id == a.OpportunityId)
                .Select(o => (Guid?)o.Venue.UserId)
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ApplicationEntity>> GetRecentDeniedByArtistIdAsync(int artistId)
    {
        return await (
            from a in context.Applications.Include(a => a.Artist).ThenInclude(a => a.Genres)
            join opportunity in context.Opportunities on a.OpportunityId equals opportunity.Id
            where
                a.ArtistId == artistId &&
                context.Bookings.Any(b =>
                    b.OpportunityId == a.OpportunityId &&
                    b.ApplicationId != a.Id)
            orderby opportunity.Period.End descending
            select a)
            .Take(5)
            .ToListAsync();
    }

}
