using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class ContractRepository : Repository<ContractEntity>, IContractRepository
{
    private readonly BookingDbContext context;

    public ContractRepository(BookingDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await context.Contracts.SingleOrDefaultAsync(
            contract => contract.ApplicationId == applicationId,
            ct);

    public async Task<IReadOnlyDictionary<int, int>> GetIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default) =>
        await context.Contracts
            .Where(contract => applicationIds.Contains(contract.ApplicationId))
            .ToDictionaryAsync(contract => contract.ApplicationId, contract => contract.Id, ct);

    public async Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await context.Contracts
            .Where(contract => contract.ApplicationId == applicationId)
            .Select(contract => (int?)contract.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        await context.Contracts.SingleOrDefaultAsync(contract => contract.BookingId == bookingId, ct);
}
