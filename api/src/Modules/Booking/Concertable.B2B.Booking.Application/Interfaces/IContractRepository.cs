using Concertable.B2B.Booking.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IContractRepository : IRepository<ContractEntity, int>
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
