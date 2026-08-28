using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class BookingRepository : VenueArtistTenantScopedRepository<BookingEntity>, IBookingRepository
{
    private readonly BookingDbContext context;

    public BookingRepository(BookingDbContext context) : base(context) =>
        this.context = context;

    public Task<BookingEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Bookings.SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BookingEntity>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        await context.Bookings
            .Where(booking => applicationIds.Contains(booking.ApplicationId))
            .ToListAsync(ct);

    public Task<BookingEntity?> GetByOperationIdAsync(
        Guid operationId,
        CancellationToken ct = default) =>
        context.Bookings.SingleOrDefaultAsync(
            booking => booking.OperationId == operationId,
            ct);

    public Task<int?> GetApplicationIdByIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.Id == bookingId)
            .Select(booking => (int?)booking.ApplicationId)
            .FirstOrDefaultAsync(ct);

    public Task<int> GetAwaitingCheckoutCountByArtistTenantIdAsync(
        Guid artistTenantId,
        DateTime now,
        CancellationToken ct = default) =>
        context.Bookings.CountAsync(
            booking =>
                booking.ArtistTenantId == artistTenantId &&
                booking.EndDate > now &&
                booking.DealType != DealType.VenueHire &&
                (booking.State == State.AwaitingConfirmation ||
                 booking.State == State.ConfirmationFailed),
            ct);

    public Task<bool> TrySaveChangesAsync(CancellationToken ct = default) =>
        context.TrySaveChangesAsync(ct);
}
