using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository repository;
    private readonly IApplicationRepository applicationRepository;

    public BookingService(IBookingRepository repository, IApplicationRepository applicationRepository)
    {
        this.repository = repository;
        this.applicationRepository = applicationRepository;
    }

    public async Task<StandardBookingDto> CreateStandardAsync(int applicationId, DealType dealType)
    {
        var booking = StandardBooking.Create(applicationId, dealType);
        await InheritTenantsAsync(booking, applicationId);
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, DealType dealType, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(applicationId, dealType, paymentMethodId);
        await InheritTenantsAsync(booking, applicationId);
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    private async Task InheritTenantsAsync(BookingEntity booking, int applicationId)
    {
        var (venueTenantId, artistTenantId) = await applicationRepository.GetTenantPairAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        booking.VenueTenantId = venueTenantId;
        booking.ArtistTenantId = artistTenantId;
    }

    public async Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId)
    {
        var booking = await repository.GetForSettlementByConcertIdAsync(concertId)
            .OrNotFound();
        if (booking is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");
        return deferred.ToSettlement();
    }
}
