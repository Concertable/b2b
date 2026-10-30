using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal interface IBookingAuthorizer
{
    Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, BookingAccessScope scope, CancellationToken ct = default);
}

internal sealed class BookingAuthorizer(
    BookingPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor,
    IAuthorizationContext membership,
    IMembershipReadRepository memberships,
    TimeProvider clock) : IBookingAuthorizer
{
    public async Task<UnitResult<AccessDenied>> AuthorizeAsync(
        int id, TenantPermission permission, BookingAccessScope scope, CancellationToken ct = default)
    {
        if (membership.Membership is not { } expected
            || await memberships.LockIfUnchangedAsync(expected, ct) is not { } actor
            || !actor.HasPermission(permission))
            return new AccessDenied();

        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Booking authorization requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."Bookings" WHERE "Id" = {id} FOR UPDATE""", ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM booking."BookingAccessGrants" WHERE "ResourceId" = {id} ORDER BY "Id" FOR UPDATE""", ct);

        return await context.Bookings.AsNoTracking()
            .AccessibleBy(actor, permission, scope, clock.GetUtcNow().UtcDateTime)
            .AnyAsync(booking => booking.Id == id, ct)
            ? UnitResult.Success<AccessDenied>()
            : new AccessDenied();
    }
}
