using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Domain.Entities;

public sealed class BookingAccessGrant : ResourceAccessGrant<BookingAccessScope>
{
    private BookingAccessGrant() { }

    internal static BookingAccessGrant Issue(
        int bookingId,
        Guid tenantId,
        Guid? membershipId,
        BookingAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new BookingAccessGrant();
        grant.Initialize(
            bookingId,
            tenantId,
            membershipId,
            scope,
            issuedByTenantId,
            issuedByUserId,
            kind,
            at,
            validUntil);
        return grant;
    }

    internal void Revoke(DateTime at) => RevokeCore(at);
}
