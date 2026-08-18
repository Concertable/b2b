using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Booking.Api.Controllers;

[ApiController]
[Route("api/booking")]
internal sealed class BookingController(IBookingService bookings) : ControllerBase
{
    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> Cancel(int bookingId, CancellationToken ct) =>
        (await bookings.CancelAsync(bookingId, ct)).ToNoContentOrProblem();
}
