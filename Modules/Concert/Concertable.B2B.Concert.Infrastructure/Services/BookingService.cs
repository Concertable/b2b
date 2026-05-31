using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal class BookingService : IBookingService
{
    private readonly IBookingRepository bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        this.bookingRepository = bookingRepository;
    }

    public async Task<StandardBookingDto> CreateStandardAsync(int applicationId)
    {
        var booking = StandardBooking.Create(applicationId);
        booking.AwaitPayment();
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(applicationId, paymentMethodId);
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingSettlement> MarkAwaitingPaymentByConcertIdAsync(int concertId)
    {
        var booking = await bookingRepository.GetForSettlementByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        if (booking is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");
        deferred.AwaitPayment();
        await bookingRepository.SaveChangesAsync();
        return deferred.ToSettlement();
    }

    public async Task<IBooking> CompleteByConcertIdAsync(int concertId)
    {
        var booking = await bookingRepository.GetForCompletionByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        booking.Complete();
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task CompleteAsync(int bookingId)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking not found");
        booking.Complete();
        await bookingRepository.SaveChangesAsync();
    }

    public async Task FailPaymentAsync(int bookingId, CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking not found");
        booking.FailPayment();
        await bookingRepository.SaveChangesAsync();
    }
}
