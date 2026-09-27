using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class MembershipService : IMembershipService
{
    private readonly IMembershipRepository repository;
    private readonly ITenantRepository tenantRepository;
    private readonly ITenantContext tenantContext;
    private readonly IMembershipContext membershipContext;
    private readonly IUserModule userModule;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;

    public MembershipService(
        IMembershipRepository repository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IMembershipContext membershipContext,
        IUserModule userModule,
        IOutboxUnitOfWorkBehavior unitOfWork)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.tenantContext = tenantContext;
        this.membershipContext = membershipContext;
        this.userModule = userModule;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MemberDto>> ListMembersAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var memberships = await repository.ListMembershipsByTenantAsync(tenantId, ct);
        var emails = await userModule.GetEmailsByIdsAsync(memberships.Select(m => m.UserId));
        return memberships
            .Select(m => new MemberDto(m.UserId, emails[m.UserId], m.Role))
            .ToList();
    }

    public Task<UnitResult<ChangeMemberRoleError>> ChangeRoleAsync(
        Guid userId,
        ChangeMemberRoleRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => ChangeRoleCoreAsync(userId, request, ct), ct);

    private async Task<UnitResult<ChangeMemberRoleError>> ChangeRoleCoreAsync(
        Guid userId,
        ChangeMemberRoleRequest request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null
            || await GetCurrentOwnerAsync(tenantId, ct) is null)
            return new ChangeMemberRoleError.NotPermitted();

        var membership = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (membership is null)
            return new ChangeMemberRoleError.MemberNotFound(userId);

        if (membership.Role == TenantRole.Owner
            && request.Role != TenantRole.Owner
            && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new ChangeMemberRoleError.LastOwner();

        membership.ChangeRole(request.Role);
        return new Success();
    }

    public Task<UnitResult<RemoveMemberError>> RemoveMemberAsync(
        Guid userId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RemoveMemberCoreAsync(userId, ct), ct);

    private async Task<UnitResult<RemoveMemberError>> RemoveMemberCoreAsync(
        Guid userId,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null
            || await GetCurrentOwnerAsync(tenantId, ct) is null)
            return new RemoveMemberError.NotPermitted();

        var membership = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (membership is null)
            return new RemoveMemberError.MemberNotFound(userId);

        if (membership.Role == TenantRole.Owner && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new RemoveMemberError.LastOwner();

        repository.Remove(membership);
        return new Success();
    }

    private async Task<TenantMembershipEntity?> GetCurrentOwnerAsync(Guid tenantId, CancellationToken ct)
    {
        var expected = membershipContext.Membership;
        if (expected is null || expected.TenantId != tenantId || expected.Role != TenantRole.Owner)
            return null;

        var current = await repository.FindMembershipByIdAsync(tenantId, expected.MembershipId, ct);
        return current is not null
            && current.UserId == expected.UserId
            && current.Role == TenantRole.Owner
            && current.PermissionVersion == expected.PermissionVersion
                ? current
                : null;
    }
}
