using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal interface IApplicationAuthorizer
{
    Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ApplicationAccessScope scope, CancellationToken ct = default);
}

internal sealed class ApplicationAuthorizer(
    ApplicationPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IAuthorizationContext membership,
    IMembershipReadRepository memberships,
    TimeProvider clock) : IApplicationAuthorizer
{
    public async Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ApplicationAccessScope scope, CancellationToken ct = default)
    {
        if (membership.Membership is not { } expected
            || await memberships.LockIfUnchangedAsync(expected, ct) is not { } actor
            || !actor.HasPermission(permission))
            return new AccessDenied();

        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Application authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        var ids = new[] { id };
        if (permission == TenantPermission.ApplicationsDecide)
        {
            var opportunityId = await context.Applications.AsNoTracking()
                .Where(application => application.Id == id)
                .Select(application => (int?)application.OpportunityId)
                .SingleOrDefaultAsync(ct);
            if (opportunityId is { } siblingsOf)
                ids = await context.Applications.AsNoTracking()
                    .Where(application => application.OpportunityId == siblingsOf)
                    .OrderBy(application => application.Id)
                    .Select(application => application.Id)
                    .ToArrayAsync(ct);
        }
        foreach (var locked in ids)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM application."Applications" WHERE "Id" = {locked} FOR UPDATE""", ct);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM application."ApplicationAccessGrants" WHERE "ResourceId" = {locked} ORDER BY "Id" FOR UPDATE""", ct);
        }

        return await context.Applications.AsNoTracking()
            .AccessibleBy(actor, permission, scope, clock.GetUtcNow().UtcDateTime)
            .AnyAsync(application => application.Id == id, ct)
            ? UnitResult.Success<AccessDenied>()
            : new AccessDenied();
    }
}
