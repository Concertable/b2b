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

    public async Task<StandardBookingDto> CreateStandardAsync(int applicationId, ContractType contractType)
    {
        var booking = StandardBooking.Create(applicationId, contractType);
        await InheritTenantsAsync(booking, applicationId);
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    public async Task<DeferredBookingDto> CreateDeferredAsync(int applicationId, ContractType contractType, string paymentMethodId)
    {
        var booking = DeferredBooking.Create(applicationId, contractType, paymentMethodId);
        await InheritTenantsAsync(booking, applicationId);
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();
        return booking.ToDto();
    }

    private async Task InheritTenantsAsync(BookingEntity booking, int applicationId)
    {
        var (venueTenantId, artistTenantId) = await applicationRepository.GetTenantPairAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        booking.VenueTenantId = venueTenantId;
        booking.ArtistTenantId = artistTenantId;
    }

    public async Task<BookingSettlement> GetSettlementByConcertIdAsync(int concertId)
    {
        var booking = await repository.GetForSettlementByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");
        if (booking is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");
        return deferred.ToSettlement();
    }
}
