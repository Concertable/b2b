using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Booking.Api.Controllers;

[ApiController]
[Route("api/booking")]
internal sealed class BookingController : ControllerBase
{
    private readonly IBookingService bookingService;

    public BookingController(IBookingService bookingService) => this.bookingService = bookingService;

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<BookingSummary>> GetByApplicationId(
        int applicationId,
        CancellationToken ct)
    {
        var booking = await bookingService.GetSummaryByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? NotFound()
            : Ok(booking.ToSummary());
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> Cancel(int bookingId, CancellationToken ct) =>
        (await bookingService.CancelAsync(bookingId, ct)).ToNoContentOrProblem();
}
