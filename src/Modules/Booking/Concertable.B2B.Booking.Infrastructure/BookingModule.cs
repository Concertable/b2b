using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Infrastructure;

internal sealed class BookingModule : IBookingModule
{
    private readonly IBookingService bookingService;
    private readonly IContractService contractService;

    public BookingModule(
        IBookingService bookingService,
        IContractService contractService)
    {
        this.bookingService = bookingService;
        this.contractService = contractService;
    }

    public async Task<Option<BookingSummary>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingService.GetSummaryByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? Option.None<BookingSummary>()
            : Option.Some(new BookingSummary(
                booking.Id,
                booking.ApplicationId,
                Map(booking.State)));
    }

    public async Task<Option<int>> GetContractIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (await contractService.GetIdByApplicationIdAsync(applicationId, ct)).ToOption();

    public async Task<Option<ContractPdf>> GetContractPdfByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default)
    {
        var result = await contractService.GetPdfByBookingIdAsync(bookingId, ct);
        return result.TryGetValue(out var pdf)
            ? Option.Some(new ContractPdf(pdf.Content, pdf.FileName, pdf.ContentType))
            : Option.None<ContractPdf>();
    }

    private static BookingStatus Map(BookingState state) => state switch
    {
        BookingState.AwaitingConfirmation => BookingStatus.AwaitingConfirmation,
        BookingState.ConfirmationFailed => BookingStatus.ConfirmationFailed,
        BookingState.Confirmed => BookingStatus.Confirmed,
        BookingState.CancellationPending => BookingStatus.CancellationPending,
        BookingState.CancellationFailed => BookingStatus.CancellationFailed,
        BookingState.Cancelled => BookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };
}
