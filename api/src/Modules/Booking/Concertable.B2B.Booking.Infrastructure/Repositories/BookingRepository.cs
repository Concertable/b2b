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
    private readonly IPermissionAuthorization permissions;
    private readonly TimeProvider clock;

    public BookingRepository(BookingDbContext context, IMembershipContext membership,
        IPermissionAuthorization permissions, TimeProvider clock) : base(context)
    {
        this.context = context;
        this.membership = membership;
        this.permissions = permissions;
        this.clock = clock;
    }

    public async ValueTask AddContractAsync(ContractEntity contract, CancellationToken ct = default) =>
        await context.Contracts.AddAsync(contract, ct);

    public async Task<BookingEntity?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await (await WithFacetAsync(ResourceFacet.Summary, ct)).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public async Task<BookingEntity?> GetOperationsByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await (await WithFacetAsync(ResourceFacet.Operations, ct)).SingleOrDefaultAsync(
            booking => booking.ApplicationId == applicationId,
            ct);

    public async Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await (await WithFacetAsync(ResourceFacet.Summary, ct))
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BookingEntity>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        await (await WithFacetAsync(ResourceFacet.Summary, ct))
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
        await (await WithFacetAsync(ResourceFacet.Operations, ct)).CountAsync(
            booking =>
                booking.ArtistTenantId == artistTenantId &&
                booking.EndDate > now &&
                booking.DealType != DealType.VenueHire &&
                (booking.State == BookingState.AwaitingConfirmation ||
                 booking.State == BookingState.ConfirmationFailed),
            ct);

    private async Task<IQueryable<BookingEntity>> WithFacetAsync(ResourceFacet facet, CancellationToken ct)
    {
        var actor = membership.Membership;
        if (actor is null || await permissions.CheckAsync(TenantPermission.OperationsView, ct: ct)
            != AuthorizationDecision.Allowed)
            return context.Bookings.Where(_ => false);

        var binding = ResourcePolicyBinding.FromCatalog(
            TenantPermission.OperationsView, ResourceKind.Booking, facet);
        return BookingAuthorizationPolicy.VisibleBookings(
            context.Bookings.IgnoreQueryFilters([TenantFilters.Key]),
            context.BookingAccessGrants.IgnoreQueryFilters([TenantFilters.Key]),
            context.MembershipAuthority.AsNoTracking(), actor, binding, clock.GetUtcNow().UtcDateTime);
    }
}
