using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class ContractRepository : Repository<ContractEntity>, IContractRepository
{
    private readonly BookingDbContext context;

    public ContractRepository(BookingDbContext context) : base(context) =>
        this.context = context;

    public Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Contracts.SingleOrDefaultAsync(
            contract => contract.ApplicationId == applicationId,
            ct);

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Contracts
            .Where(contract => contract.ApplicationId == applicationId)
            .Select(contract => (int?)contract.Id)
            .SingleOrDefaultAsync(ct);

    public Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        context.Contracts.SingleOrDefaultAsync(contract => contract.BookingId == bookingId, ct);
}
