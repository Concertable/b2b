using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;

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

    public Task<int> GetArtistAwaitingCheckoutCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        bookingService.GetArtistAwaitingCheckoutCountAsync(artistTenantId, ct);

    public async Task RecordPaymentVerificationAsync(
        BookingPaymentVerification verification,
        CancellationToken ct = default)
    {
        var bookingId = await bookingService.GetIdByApplicationIdAsync(verification.ApplicationId, ct);
        if (bookingId is null)
            return;

        switch (verification)
        {
            case SuccessfulBookingPaymentVerification succeeded:
                await bookingService.RecordSucceededAsync(
                    bookingId.Value,
                    new VerifyPaymentSucceededEvidence(succeeded.ApplicationId, succeeded.ProviderTransactionId),
                    ct);
                return;
            case FailedBookingPaymentVerification failed:
                await bookingService.RecordFailedAsync(
                    bookingId.Value,
                    new VerifyPaymentFailedEvidence(
                        failed.ApplicationId,
                        failed.ProviderTransactionId,
                        new FinancialOperationError(failed.Code, failed.Message)),
                    ct);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(verification), verification, null);
        }
    }

}
