using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class ContractRepository : Repository<ContractEntity>, IContractRepository
{
    private readonly BookingDbContext context;
    private readonly IMembershipContext membership;
    private readonly IPermissionAuthorization permissions;
    private readonly TimeProvider clock;

    public ContractRepository(BookingDbContext context, IMembershipContext membership,
        IPermissionAuthorization permissions, TimeProvider clock) : base(context)
    {
        this.context = context;
        this.membership = membership;
        this.permissions = permissions;
        this.clock = clock;
    }

    public async Task<ContractEntity?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await (await WithReadAsync(ct)).SingleOrDefaultAsync(
            contract => contract.ApplicationId == applicationId,
            ct);

    public async Task<IReadOnlyDictionary<int, int>> GetIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default) =>
        await (await WithReadAsync(ct))
            .Where(contract => applicationIds.Contains(contract.ApplicationId))
            .ToDictionaryAsync(contract => contract.ApplicationId, contract => contract.Id, ct);

    public async Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        await (await WithReadAsync(ct))
            .Where(contract => contract.ApplicationId == applicationId)
            .Select(contract => (int?)contract.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<ContractEntity?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        await (await WithReadAsync(ct)).SingleOrDefaultAsync(contract => contract.BookingId == bookingId, ct);
    private async Task<IQueryable<ContractEntity>> WithReadAsync(CancellationToken ct)
    {
        var actor = membership.Membership;
        if (actor is null || await permissions.CheckAsync(TenantPermission.TermsRead, ct: ct)
            != AuthorizationDecision.Allowed)
            return context.Contracts.Where(_ => false);

        var binding = ResourcePolicyBinding.FromCatalog(
            TenantPermission.TermsRead, ResourceKind.Contract, ResourceFacet.Read);
        return BookingGrantPolicy.VisibleContracts(
            context.Contracts.IgnoreQueryFilters([TenantFilters.Key]),
            context.ContractAccessGrants.IgnoreQueryFilters([TenantFilters.Key]),
            context.MembershipAuthority.AsNoTracking(), actor, binding, clock.GetUtcNow().UtcDateTime);
    }
}
