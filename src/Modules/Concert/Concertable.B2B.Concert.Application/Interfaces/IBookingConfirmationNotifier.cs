using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingConfirmationNotifier
{
    Task BookingConfirmedAsync(Guid venueTenantId, string venueName, Guid artistTenantId, string artistName, DateRange period);
}
