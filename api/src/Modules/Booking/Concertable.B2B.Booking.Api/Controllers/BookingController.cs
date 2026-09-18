using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
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

    [HasPermission(TenantPermission.OperationsView)]
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

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpPost("{bookingId:int}/shares")]
    public async Task<ActionResult<BookingShareResponse>> Share(
        int bookingId,
        [FromBody] ShareBookingRequest request,
        CancellationToken ct) =>
        (await bookingService.ShareAsync(bookingId, request, ct)).ToOkOrProblem();

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpDelete("{bookingId:int}/shares/{grantId:guid}")]
    public async Task<IActionResult> RevokeShare(
        int bookingId,
        Guid grantId,
        CancellationToken ct) =>
        (await bookingService.RevokeShareAsync(bookingId, grantId, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> Cancel(int bookingId, CancellationToken ct) =>
        (await bookingService.CancelAsync(bookingId, ct)).ToNoContentOrProblem();
}
