using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class ContractRepository : Repository<ContractEntity>, IContractRepository
{
    private readonly BookingDbContext context;
    private readonly IMembershipContext membership;
    private readonly TimeProvider clock;

    public ContractRepository(BookingDbContext context, IMembershipContext membership, TimeProvider clock)
        : base(context)
    {
        this.context = context;
        this.membership = membership;
        this.clock = clock;
    }

    public async Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await Readable().SingleOrDefaultAsync(
            contract => contract.ApplicationId == applicationId,
            ct);

    public async Task<IReadOnlyDictionary<int, int>> GetIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default) =>
        await Readable()
            .Where(contract => applicationIds.Contains(contract.ApplicationId))
            .ToDictionaryAsync(contract => contract.ApplicationId, contract => contract.Id, ct);

    public async Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await Readable()
            .Where(contract => contract.ApplicationId == applicationId)
            .Select(contract => (int?)contract.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        await Readable().SingleOrDefaultAsync(contract => contract.BookingId == bookingId, ct);
    private IQueryable<ContractEntity> Readable() =>
        membership.Membership is { } actor
            ? context.Contracts.IgnoreQueryFilters([TenantFilters.Key])
                .AccessibleBy(actor, TenantPermission.TermsRead, ContractAccessScope.Read, clock.GetUtcNow().UtcDateTime)
                .WhereCurrentMember(context.MembershipAuthority.AsNoTracking(), actor)
            : context.Contracts.Where(_ => false);
}
