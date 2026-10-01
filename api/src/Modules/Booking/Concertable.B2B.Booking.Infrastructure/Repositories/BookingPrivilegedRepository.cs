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
    public async Task<IReadOnlyList<Guid>> GetPartyTenantIdsAsync(
        int bookingId, CancellationToken ct = default)
    {
        var booking = await context.Bookings.AsNoTracking()
            .Where(candidate => candidate.Id == bookingId)
            .Select(candidate => new { candidate.VenueTenantId, candidate.ArtistTenantId })
            .SingleOrDefaultAsync(ct);
        return booking is null ? [] : [booking.VenueTenantId, booking.ArtistTenantId];
    }

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

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    private async Task LockResourceAndGrantsAsync(int bookingId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM booking."Bookings"
             WHERE "Id" = {bookingId}
             FOR UPDATE
             """,
            ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM booking."BookingAccessGrants"
             WHERE "ResourceId" = {bookingId}
             FOR UPDATE
             """,
            ct);
    }
}
