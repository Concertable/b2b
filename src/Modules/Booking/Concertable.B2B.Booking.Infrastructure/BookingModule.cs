using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Application.Mappers;

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
            : Option.Some(booking.ToSummary());
    }

    public async Task<IReadOnlyList<BookingSummary>> GetByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        (await bookingService.GetSummariesByApplicationIdsAsync(applicationIds, ct))
            .Select(booking => booking.ToSummary())
            .ToList();

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

}
