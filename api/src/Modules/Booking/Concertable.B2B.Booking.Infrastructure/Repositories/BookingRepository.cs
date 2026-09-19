using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class BookingRepository : Repository<BookingEntity>, IBookingRepository
{
    private readonly BookingDbContext context;

    public BookingRepository(BookingDbContext context) : base(context) =>
        this.context = context;

    public Task<BookingEntity?> GetWithGrantsByIdAsync(int id, CancellationToken ct = default) =>
        context.Bookings
            .Include(booking => booking.AccessGrants)
            .FirstOrDefaultAsync(booking => booking.Id == id, ct);

    public async ValueTask AddContractAsync(ContractEntity contract, CancellationToken ct = default) =>
        await context.Contracts.AddAsync(contract, ct);

    public Task<BookingEntity?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        WithScope(BookingAccessScope.Summary).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public Task<BookingEntity?> GetOperationsByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        WithScope(BookingAccessScope.Operations).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        WithScope(BookingAccessScope.Summary)
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BookingEntity>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        await WithScope(BookingAccessScope.Summary)
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

    public Task<BookingState?> GetStateByIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.Id == bookingId)
            .Select(booking => (BookingState?)booking.State)
            .FirstOrDefaultAsync(ct);

    public Task<int> GetAwaitingCheckoutCountByArtistTenantIdAsync(
        Guid artistTenantId,
        DateTime now,
        CancellationToken ct = default) =>
        WithScope(BookingAccessScope.Operations).CountAsync(
            booking =>
                booking.ArtistTenantId == artistTenantId &&
                booking.EndDate > now &&
                booking.DealType != DealType.VenueHire &&
                (booking.State == BookingState.AwaitingConfirmation ||
                 booking.State == BookingState.ConfirmationFailed),
            ct);

    private IQueryable<BookingEntity> WithScope(BookingAccessScope scope) =>
        context.Bookings.Where(booking =>
            context.BookingAccessGrants.Any(grant =>
                grant.ResourceId == booking.Id && grant.Scope == scope));

}
