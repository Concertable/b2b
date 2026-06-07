using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        this.bookingRepository = bookingRepository;
    }

    public async Task<StandardBookingDto> CreateStandardAsync(int applicationId, ContractType contractType)
    {
        var booking = StandardBooking.Create(applicationId, contractType);
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, ContractType contractType, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(applicationId, contractType, paymentMethodId);
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId)
    {
        var booking = await bookingRepository.GetForSettlementByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        if (booking is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");
        return deferred.ToSettlement();
    }
}
