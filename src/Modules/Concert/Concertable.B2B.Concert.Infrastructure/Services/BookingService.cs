using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository repository;

    public BookingService(IBookingRepository repository)
    {
        this.repository = repository;
    }

    public async Task<StandardBookingDto> CreateStandardAsync(ApplicationEntity application)
    {
        var booking = StandardBooking.Create(ToAcceptedApplication(application));
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<DeferredBookingDto> CreateDeferredAsync(ApplicationEntity application, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(ToAcceptedApplication(application), paymentMethodId);
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId)
    {
        var booking = await repository.GetForSettlementByConcertIdAsync(concertId)
            .OrNotFound();
        if (booking is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");
        return deferred.ToSettlement();
    }

    private static AcceptedApplication ToAcceptedApplication(ApplicationEntity application) =>
        new(
            application.BeginAcceptance(),
            application.Id,
            application.OpportunityId,
            application.ArtistId,
            application.VenueTenantId,
            application.ArtistTenantId,
            application.DealType);
}
