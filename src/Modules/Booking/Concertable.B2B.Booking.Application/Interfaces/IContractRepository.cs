using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IContractRepository : IVenueArtistTenantScopedRepository<ContractEntity>
{
    Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default);
    Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
}
