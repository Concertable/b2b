using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal sealed class ApplicationPrivilegedRepository(ApplicationPrivilegedDbContext context)
    : IApplicationPrivilegedRepository
{
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

    public Task<bool> AnyAcceptedByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        context.Applications.AnyAsync(
            application =>
                application.OpportunityId == opportunityId
                && application.State == ApplicationState.Accepted,
            ct);

    public async Task<IReadOnlyList<int>> RejectAllExceptAsync(
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

        return applications.Select(application => application.Id).ToList();
    }

    private Task LockAsync(int applicationId, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM application.Applications WITH (UPDLOCK, HOLDLOCK)
             WHERE Id = {applicationId}
             """,
            ct);
}
