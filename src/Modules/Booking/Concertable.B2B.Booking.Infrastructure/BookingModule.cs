using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Infrastructure;

internal sealed class BookingModule(
    IBookingRepository bookings,
    IContractRepository contracts) : IBookingModule
{
    public async Task<Option<BookingSummary>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetByApplicationIdAsync(applicationId, ct);
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
        (await contracts.GetIdByApplicationIdAsync(applicationId, ct)).ToOption();

    private static BookingStatus Map(BookingState state) => state switch
    {
        BookingState.AwaitingFinancialConfirmation => BookingStatus.AwaitingFinancialConfirmation,
        BookingState.FinancialConfirmationFailed => BookingStatus.FinancialConfirmationFailed,
        BookingState.Confirmed => BookingStatus.Confirmed,
        BookingState.CancellationPending => BookingStatus.CancellationPending,
        BookingState.CancellationFailed => BookingStatus.CancellationFailed,
        BookingState.Cancelled => BookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };
}
