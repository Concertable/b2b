using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal sealed class ApplicationRepository : VenueArtistTenantScopedRepository<ApplicationEntity>, IApplicationRepository
{
    private readonly ApplicationDbContext context;

    public ApplicationRepository(ApplicationDbContext context) : base(context) =>
        this.context = context;

    public async Task<IReadOnlyList<ApplicationEntity>> GetByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application => application.OpportunityId == opportunityId)
            .ToListAsync(ct);

    public Task<bool> ExistsForOpportunityAndArtistTenantAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            a => a.OpportunityId == opportunityId
                && a.ArtistTenantId == artistTenantId,
            ct);

    public async Task<IReadOnlyList<ApplicationEntity>> GetByArtistTenantIdAndStateAsync(
        Guid artistTenantId,
        ApplicationState state,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                application.State == state)
            .ToListAsync(ct);

    public async Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var row = await context.Applications
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.VenueTenantId, a.ArtistTenantId })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : (row.VenueTenantId, row.ArtistTenantId);
    }

    public Task<ApplicationEntity?> GetWithVerifyPaymentByIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Applications
            .Include(application => application.VerifyPayment)
            .SingleOrDefaultAsync(application => application.Id == applicationId, ct);

    public async Task RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.OpportunityId == opportunityId &&
                application.Id != applicationId &&
                application.State == ApplicationState.Applied)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    application => application.State,
                    ApplicationState.Rejected),
                ct);

    public async Task<IReadOnlyList<ApplicationDashboardProjection>> GetVenueDashboardProjectionsAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.VenueTenantId == venueTenantId &&
                application.State == ApplicationState.Applied)
            .Select(application => new ApplicationDashboardProjection(
                application.OpportunityId,
                application.State,
                application.DealType))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ApplicationDashboardProjection>> GetArtistDashboardProjectionsAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                (application.State == ApplicationState.Applied ||
                 application.State == ApplicationState.Accepted))
            .Select(application => new ApplicationDashboardProjection(
                application.OpportunityId,
                application.State,
                application.DealType))
            .ToListAsync(ct);
}
