using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingTenantDeletionGuard : ITenantDeletionGuard
{
    private readonly IBookingPrivilegedRepository bookingPrivilegedRepository;

    public BookingTenantDeletionGuard(IBookingPrivilegedRepository bookingPrivilegedRepository)
    {
        this.bookingPrivilegedRepository = bookingPrivilegedRepository;
    }

    public Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default) =>
        bookingPrivilegedRepository.ExistsByTenantIdAsync(tenantId, ct);
}
