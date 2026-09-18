using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class BookingPrivilegedRepository(BookingPrivilegedDbContext context)
    : IBookingPrivilegedRepository
{
    public Task<BookingEntity?> GetByIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Bookings.SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);

    public async Task<BookingEntity?> GetByIdForUpdateAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        await LockResourceAndGrantsAsync(bookingId, ct);
        return await context.Bookings
            .Include(booking => booking.AccessGrants)
            .SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);
    }

    public Task<BookingEntity?> GetWithContractByIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Bookings
            .Include(booking => booking.Contract)
            .SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);

    public async Task<BookingEntity?> GetWithContractByIdForUpdateAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        await LockResourceAndGrantsAsync(bookingId, ct);
        return await context.Bookings
            .Include(booking => booking.Contract)
            .SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);
    }

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public Task<BookingState?> GetStateByIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.Id == bookingId)
            .Select(booking => (BookingState?)booking.State)
            .SingleOrDefaultAsync(ct);

    public Task<bool> CanCancelAsync(
        int bookingId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default) =>
        context.Bookings.AsNoTracking().AnyAsync(booking =>
            booking.Id == bookingId
            && (booking.VenueTenantId == actor.TenantId || booking.ArtistTenantId == actor.TenantId)
            && context.BookingAccessGrants.Any(grant =>
                grant.ResourceId == booking.Id
                && grant.Scope == BookingAccessScope.Operations
                && grant.TenantId == actor.TenantId
                && grant.RevokedAt == null
                && grant.ValidFrom <= at
                && (grant.ValidUntil == null || at < grant.ValidUntil)
                && (audience == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                    || audience == ResourceAudience.AssignedResources
                        && grant.MembershipId == actor.MembershipId)),
            ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    private async Task LockResourceAndGrantsAsync(int bookingId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM booking.Bookings WITH (UPDLOCK, HOLDLOCK)
             WHERE Id = {bookingId}
             """,
            ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM booking.BookingAccessGrants WITH (UPDLOCK, HOLDLOCK)
             WHERE ResourceId = {bookingId}
             """,
            ct);
    }
}
