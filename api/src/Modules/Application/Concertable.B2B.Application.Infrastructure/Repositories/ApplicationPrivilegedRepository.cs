using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal sealed class ApplicationPrivilegedRepository(
    ApplicationPrivilegedDbContext context, UnitOfWorkAccessor unitOfWorkAccessor)
    : IApplicationPrivilegedRepository
{
    public async Task AddAsync(ApplicationEntity application, CancellationToken ct = default) =>
        await context.Applications.AddAsync(application, ct);

    public async Task LockOpportunityAsync(int opportunityId, CancellationToken ct = default)
    {
        if (opportunityId <= 0)
            throw new ArgumentOutOfRangeException(nameof(opportunityId));
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("An opportunity lock requires a unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.AcquireAdvisoryLockAsync(AdvisoryLock.Opportunity, opportunityId, ct);
    }

    public async Task AcquireTenantApplicationsSharedLockAsync(
        IReadOnlySet<Guid> tenantIds,
        CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        foreach (var tenantId in tenantIds.Order())
            await context.Database.AcquireSharedAdvisoryLockAsync(AdvisoryLock.TenantApplications, tenantId, ct);
    }

    public async Task AcquireTenantApplicationsExclusiveLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        await context.Database.AcquireAdvisoryLockAsync(AdvisoryLock.TenantApplications, tenantId, ct);
    }

    public Task<bool> ExistsLiveByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Applications.AnyAsync(application =>
            (application.VenueTenantId == tenantId || application.ArtistTenantId == tenantId)
            && (application.State == ApplicationState.Applied || application.State == ApplicationState.Accepted),
            ct);

    public Task<bool> ExistsByOpportunityIdAndArtistTenantIdAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            application =>
                application.OpportunityId == opportunityId
                && application.ArtistTenantId == artistTenantId,
            ct);

    public async Task<ApplicationEntity?> GetByIdWithRowLockAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        await context.Applications.LockRowsAsync(application => application.Id == applicationId, RowLock.NoKeyUpdate, ct);
        return await context.Applications.SingleOrDefaultAsync(application => application.Id == applicationId, ct);
    }

    public async Task<ApplicationEntity?> GetDecisionByIdWithRowLockAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var opportunityId = await context.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (int?)application.OpportunityId)
            .SingleOrDefaultAsync(ct);
        if (opportunityId is null)
            return null;

        await context.Applications.LockRowsAsync(
            application => application.OpportunityId == opportunityId, RowLock.NoKeyUpdate, ct);

        return await context.Applications
            .SingleOrDefaultAsync(application => application.Id == applicationId, ct);
    }

    public Task<ApplicationState?> GetStateByIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (ApplicationState?)application.State)
            .SingleOrDefaultAsync(ct);

    public Task<Guid?> GetVenueTenantIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (Guid?)application.VenueTenantId)
            .SingleOrDefaultAsync(ct);

    public Task<int?> GetOpportunityIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (int?)application.OpportunityId)
            .SingleOrDefaultAsync(ct);

    public void MarkChanged(ApplicationEntity application) =>
        context.Entry(application).Property(entity => entity.State).IsModified = true;

    public Task<bool> AnyAcceptedByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            application =>
                application.OpportunityId == opportunityId
                && application.State == ApplicationState.Accepted,
            ct);

    public async Task<IReadOnlyList<ApplicationEntity>> RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default)
    {
        var applications = await context.Applications
            .Where(application =>
                application.OpportunityId == opportunityId
                && application.Id != applicationId
                && application.State == ApplicationState.Applied)
            .ToListAsync(ct);

        foreach (var application in applications)
        {
            if (application.Reject().TryGetError(out var error))
                throw new InvalidOperationException(
                    $"Application {application.Id} could not be rejected from {error.Current}.");
            application.NotifyCounterparty(ApplicationNotification.Rejected);
        }

        return applications;
    }

    public Task<bool> OpportunityHasConcertAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability => availability.OpportunityId == opportunityId,
            ct);

    public Task<bool> ArtistHasConcertOnDateAsync(
        int artistId,
        DateTime date,
        CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability =>
                availability.ArtistId == artistId
                && availability.StartDate.Date == date.Date,
            ct);

    public Task<bool> VenueHasConcertOnDateAsync(
        int venueId,
        DateTime date,
        CancellationToken ct = default) =>
        context.ConcertAvailabilities.AnyAsync(
            availability =>
                availability.VenueId == venueId
                && availability.StartDate.Date == date.Date,
            ct);

    private Task EnlistAsync(CancellationToken ct) =>
        (unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("A tenant applications lock requires a unit of work."))
            .EnlistAsync(context, ct);
}
