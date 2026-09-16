using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Domain.Entities;

public sealed class BookingAccessGrant : ResourceAccessGrant<BookingAccessScope>
{
    private BookingAccessGrant() { }

    internal static BookingAccessGrant Issue(
        int bookingId,
        Guid tenantId,
        Guid? memberUserId,
        BookingAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new BookingAccessGrant();
        grant.Initialize(
            bookingId,
            tenantId,
            memberUserId,
            scope,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
