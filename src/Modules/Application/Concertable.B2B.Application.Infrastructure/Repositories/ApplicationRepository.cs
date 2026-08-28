using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
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
        State state,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                application.State == state)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ApplicationEntity>> GetByVenueTenantIdAndStateAsync(
        Guid venueTenantId,
        State state,
        CancellationToken ct = default) =>
        await context.Applications
            .AsNoTracking()
            .Where(application =>
                application.VenueTenantId == venueTenantId &&
                application.State == state)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ApplicationEntity>> GetCurrentByArtistTenantIdAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        await context.Applications
            .AsNoTracking()
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                application.State != State.Withdrawn)
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

    public async Task RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default)
    {
        var applications = await context.Applications
            .Where(application =>
                application.OpportunityId == opportunityId &&
                application.Id != applicationId &&
                application.State == State.Applied)
            .ToListAsync(ct);

        foreach (var application in applications)
        {
            if (application.Reject().TryGetError(out var error))
                throw new InvalidOperationException(
                    $"Application {application.Id} could not be rejected from {error.Current}.");
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ApplicationDashboardProjection>> GetVenueDashboardProjectionsAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application =>
                application.VenueTenantId == venueTenantId &&
                application.State == State.Applied)
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
                (application.State == State.Applied ||
                 application.State == State.Accepted))
            .Select(application => new ApplicationDashboardProjection(
                application.OpportunityId,
                application.State,
                application.DealType))
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        await context.Applications
            .Where(application => opportunityIds.Contains(application.OpportunityId))
            .GroupBy(application => application.OpportunityId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), ct);

    public async Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        (await context.Applications
            .Where(application => application.ArtistTenantId == artistTenantId)
            .Select(application => application.OpportunityId)
            .Distinct()
            .ToListAsync(ct))
        .ToHashSet();

    public Task<bool> TrySaveChangesAsync(CancellationToken ct = default) =>
        context.TrySaveChangesAsync(ct);
}
