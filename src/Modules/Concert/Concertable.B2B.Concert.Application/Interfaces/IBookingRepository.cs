using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingRepository : IVenueArtistTenantScopedRepository<BookingEntity>
{
    Task<BookingEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<BookingEntity?> GetByConcertIdAsync(
        int concertId,
        ISpecification<BookingEntity> spec,
        CancellationToken ct = default);
    Task<int?> GetIdByConcertIdAsync(int concertId);
}
