using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal sealed class ApplicationRepository : Repository<ApplicationEntity>, IApplicationRepository
{
    private readonly ApplicationDbContext context;

    public ApplicationRepository(ApplicationDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<ApplicationEntity?> GetSummaryByIdAsync(
        int id,
        CancellationToken ct = default) =>
        await context.Applications
            .SingleOrDefaultAsync(application => application.Id == id, ct);

    public async Task<ApplicationEntity?> GetProposalByIdAsync(
        int id,
        CancellationToken ct = default) =>
        await TermsReadable()
            .SingleOrDefaultAsync(application => application.Id == id, ct);

    public async Task<ApplicationEntity?> GetDecisionByIdAsync(
        int id,
        CancellationToken ct = default) =>
        await Decidable().SingleOrDefaultAsync(application => application.Id == id, ct);

    // Rewriting the state column with the value it already holds is what bumps the row's version; marking the
    // whole entity modified would rewrite the tenant pair, which the tenant guard rejects.
    public void MarkChanged(ApplicationEntity application) =>
        context.Entry(application).Property(entity => entity.State).IsModified = true;

    public async Task<IReadOnlyList<ApplicationEntity>> GetByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        await TermsReadable()
            .Where(application => application.OpportunityId == opportunityId)
            .ToListAsync(ct);

    public Task<bool> ExistsByOpportunityIdAndArtistTenantIdAsync(
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
        await TermsReadable()
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                application.State == state)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ApplicationEntity>> GetByVenueTenantIdAndStateAsync(
        Guid venueTenantId,
        ApplicationState state,
        CancellationToken ct = default) =>
        await TermsReadable()
            .AsNoTracking()
            .Where(application =>
                application.VenueTenantId == venueTenantId &&
                application.State == state)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ApplicationEntity>> GetCurrentByArtistTenantIdAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        await TermsReadable()
            .AsNoTracking()
            .Where(application =>
                application.ArtistTenantId == artistTenantId &&
                application.State != ApplicationState.Withdrawn)
            .ToListAsync(ct);

    public async Task<ApplicationState?> GetStateByIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await TermsReadable()
            .Where(application => application.Id == applicationId)
            .Select(application => (ApplicationState?)application.State)
            .FirstOrDefaultAsync(ct);

    public Task<bool> AnyAcceptedByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            application =>
                application.OpportunityId == opportunityId &&
                application.State == ApplicationState.Accepted,
            ct);

    public async Task<IReadOnlyList<int>> RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default)
    {
        var applications = await context.Applications
            .Where(application =>
                application.OpportunityId == opportunityId &&
                application.Id != applicationId &&
                application.State == ApplicationState.Applied)
            .ToListAsync(ct);

        foreach (var application in applications)
        {
            if (application.Reject().TryGetError(out var error))
                throw new InvalidOperationException(
                    $"Application {application.Id} could not be rejected from {error.Current}.");
            application.NotifyCounterparty(ApplicationNotification.Rejected);
        }

        await context.SaveChangesAsync(ct);
        return applications.Select(application => application.Id).ToList();
    }

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

    public async Task<IReadOnlySet<int>> GetDecidableIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default) =>
        context.Actor is { } actor
            ? (await Unfiltered(applicationIds).ToListAsync(ct))
                .Where(application => application.IsDecidableBy(actor))
                .Select(application => application.Id)
                .ToHashSet()
            : new HashSet<int>();

    public async Task<IReadOnlySet<int>> GetWithdrawableIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default) =>
        context.Actor is { } actor
            ? (await Unfiltered(applicationIds).ToListAsync(ct))
                .Where(application => application.IsWithdrawableBy(actor))
                .Select(application => application.Id)
                .ToHashSet()
            : new HashSet<int>();

    private IQueryable<ApplicationEntity> Unfiltered(IReadOnlyCollection<int> applicationIds) =>
        context.Applications.IgnoreQueryFilters([TenantFilters.Key]).AsNoTracking()
            .Where(application => applicationIds.Contains(application.Id));

    private IQueryable<ApplicationEntity> Decidable() =>
        context.Actor is { } actor
        && actor.Permissions[TenantPermission.ApplicationsDecide] == ResourceAccess.All
            ? context.Applications.IgnoreQueryFilters([TenantFilters.Key])
                .Where(application => application.VenueTenantId == actor.TenantId)
            : context.Applications.Where(_ => false);

    private IQueryable<ApplicationEntity> TermsReadable() =>
        context.Actor is { } actor
        && actor.Permissions[TenantPermission.TermsRead] == ResourceAccess.All
            ? context.Applications.IgnoreQueryFilters([TenantFilters.Key])
                .Where(application =>
                    application.VenueTenantId == actor.TenantId || application.ArtistTenantId == actor.TenantId)
            : context.Applications.Where(_ => false);
}
