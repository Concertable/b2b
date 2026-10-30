using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal interface IConcertAuthorizer
{
    Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ConcertAccessScope? scope, CancellationToken ct = default);
}

internal sealed class ConcertAuthorizer(
    ConcertPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IAuthorizationContext membership,
    IMembershipReadRepository memberships,
    TimeProvider clock) : IConcertAuthorizer
{
    public async Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, ConcertAccessScope? scope, CancellationToken ct = default)
    {
        if (membership.Membership is not { } expected
            || await memberships.LockIfUnchangedAsync(expected, ct) is not { } actor
            || !actor.HasPermission(permission))
            return new AccessDenied();

        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Concert authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."Concerts" WHERE "Id" = {id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM concert."ConcertAccessGrants" WHERE "ResourceId" = {id} ORDER BY "Id" FOR UPDATE""", ct);

        return await context.Concerts.AsNoTracking()
            .AccessibleBy(actor, permission, scope, clock.GetUtcNow().UtcDateTime)
            .AnyAsync(concert => concert.Id == id, ct)
            ? UnitResult.Success<AccessDenied>()
            : new AccessDenied();
    }
}
