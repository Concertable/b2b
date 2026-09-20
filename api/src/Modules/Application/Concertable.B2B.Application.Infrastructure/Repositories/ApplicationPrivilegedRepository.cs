using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Authorization.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal sealed class ApplicationPrivilegedRepository(ApplicationPrivilegedDbContext context)
    : IApplicationPrivilegedRepository
{
    public async Task AddAsync(ApplicationEntity application, CancellationToken ct = default) =>
        await context.Applications.AddAsync(application, ct);

    public Task<bool> ExistsByOpportunityIdAndArtistTenantIdAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            application =>
                application.OpportunityId == opportunityId
                && application.ArtistTenantId == artistTenantId,
            ct);

    public async Task<ApplicationEntity?> GetByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        await LockAsync(applicationId, ct);
        return await context.Applications
            .Include(application => application.AccessGrants)
            .SingleOrDefaultAsync(application => application.Id == applicationId, ct);
    }

    public async Task<ApplicationEntity?> GetDecisionByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var opportunityId = await context.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (int?)application.OpportunityId)
            .SingleOrDefaultAsync(ct);
        if (opportunityId is null)
            return null;

        var applicationIds = await context.Applications
            .Where(application => application.OpportunityId == opportunityId)
            .OrderBy(application => application.Id)
            .Select(application => application.Id)
            .ToListAsync(ct);
        foreach (var id in applicationIds)
            await LockAsync(id, ct);

        return await context.Applications
            .Include(application => application.AccessGrants)
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

    public Task<bool> CanSubmitAsync(
        int applicationId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        context.Applications.AsNoTracking().AnyAsync(application =>
            application.Id == applicationId
            && application.ArtistTenantId == actor.TenantId
            && context.ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id
                && grant.Scope == ApplicationAccessScope.Proposal
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId)),
            ct);

    public Task<bool> CanDecideAsync(
        int applicationId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        context.Applications.AsNoTracking().AnyAsync(application =>
            application.Id == applicationId
            && application.VenueTenantId == actor.TenantId
            && context.ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id
                && grant.Scope == ApplicationAccessScope.Proposal
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId)),
            ct);

    private Task LockAsync(int applicationId, CancellationToken ct) =>
        LockResourceAndGrantsAsync(applicationId, ct);

    private async Task LockResourceAndGrantsAsync(int applicationId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM application."Applications"
             WHERE "Id" = {applicationId}
             FOR UPDATE
             """,
            ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM application."ApplicationAccessGrants"
             WHERE "ResourceId" = {applicationId}
             FOR UPDATE
             """,
            ct);
    }
}
