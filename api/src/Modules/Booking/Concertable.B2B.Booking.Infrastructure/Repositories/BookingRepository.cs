using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
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
    private readonly IMembershipContext membership;
    private readonly TimeProvider clock;

    public BookingRepository(BookingDbContext context, IMembershipContext membership, TimeProvider clock)
        : base(context)
    {
        this.context = context;
        this.membership = membership;
        this.clock = clock;
    }

    public async ValueTask AddContractAsync(ContractEntity contract, CancellationToken ct = default) =>
        await context.Contracts.AddAsync(contract, ct);

    public async Task<BookingEntity?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await Accessible(BookingAccessScope.Summary).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public async Task<BookingEntity?> GetOperationsByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await Accessible(BookingAccessScope.Operations).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public async Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await Accessible(BookingAccessScope.Summary)
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BookingEntity>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        await Accessible(BookingAccessScope.Summary)
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

    public async Task<int> GetAwaitingCheckoutCountByArtistTenantIdAsync(
        Guid artistTenantId,
        DateTime now,
        CancellationToken ct = default) =>
        await Accessible(BookingAccessScope.Operations).CountAsync(
            booking =>
                booking.ArtistTenantId == artistTenantId &&
                booking.EndDate > now &&
                booking.DealType != DealType.VenueHire &&
                (booking.State == BookingState.AwaitingConfirmation ||
                 booking.State == BookingState.ConfirmationFailed),
            ct);

    public Task<bool> IsAccessibleAsync(
        int bookingId,
        TenantPermission permission,
        BookingAccessScope scope,
        CancellationToken ct = default) =>
        Accessible(permission, scope).AnyAsync(booking => booking.Id == bookingId, ct);

    private IQueryable<BookingEntity> Accessible(BookingAccessScope scope) =>
        Accessible(TenantPermission.OperationsView, scope);

    private IQueryable<BookingEntity> Accessible(TenantPermission permission, BookingAccessScope scope) =>
        membership.Membership is { } actor
            ? context.Bookings.IgnoreQueryFilters([TenantFilters.Key])
                .AccessibleBy(actor, permission, scope, clock.GetUtcNow().UtcDateTime)
                .WhereCurrentMember(context.MembershipAuthority.AsNoTracking(), actor)
            : context.Bookings.Where(_ => false);
}
