using Concertable.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.Concert.Infrastructure.Services;

internal class BookingService : IBookingService
{
    private readonly IBookingRepository bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        this.bookingRepository = bookingRepository;
    }

    public async Task<StandardBooking> CreateStandardAsync(int applicationId)
    {
        var booking = StandardBooking.Create(applicationId);
        booking.AwaitPayment();
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking;
    }

    public async Task<DeferredBooking> CreateDeferredAsync(int applicationId, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(applicationId, paymentMethodId);
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking;
    }

    public async Task<BookingEntity> MarkAwaitingPaymentByConcertIdAsync(int concertId)
    {
        var booking = await bookingRepository.GetByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        booking.AwaitPayment();
        await bookingRepository.SaveChangesAsync();
        return booking;
    }

    public async Task<BookingEntity> CompleteByConcertIdAsync(int concertId)
    {
        var booking = await bookingRepository.GetByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        booking.Complete();
        await bookingRepository.SaveChangesAsync();
        return booking;
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
