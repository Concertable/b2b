using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class MembershipService : IMembershipService
{
    private readonly ITenantRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly IUserModule userModule;

    public MembershipService(ITenantRepository repository, ITenantContext tenantContext, IUserModule userModule)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.userModule = userModule;
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

    public async Task<UnitResult<ChangeMemberRoleError>> ChangeRoleAsync(
        Guid userId,
        ChangeMemberRoleRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var membership = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (membership is null)
            return UnitResult.Failure(ChangeMemberRoleError.NotFound(userId));

        if (membership.Role == TenantRole.Owner
            && request.Role != TenantRole.Owner
            && await IsLastOwnerAsync(tenantId, ct))
        {
            return UnitResult.Failure(ChangeMemberRoleError.LastOwner);
        }

        membership.ChangeRole(request.Role);
        await repository.SaveChangesAsync(ct);
        return UnitResult.Success<ChangeMemberRoleError>();
    }

    public async Task<UnitResult<RemoveMemberError>> RemoveMemberAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var membership = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (membership is null)
            return UnitResult.Failure(RemoveMemberError.NotFound(userId));

        if (membership.Role == TenantRole.Owner && await IsLastOwnerAsync(tenantId, ct))
            return UnitResult.Failure(RemoveMemberError.LastOwner);

        repository.RemoveMembership(membership);
        await repository.SaveChangesAsync(ct);
        return UnitResult.Success<RemoveMemberError>();
    }

    // A tenant must always keep at least one Owner — only Owner holds manage-roles/remove/delete, so an ownerless tenant is unrecoverable.
    private async Task<bool> IsLastOwnerAsync(Guid tenantId, CancellationToken ct) =>
        await repository.CountOwnersAsync(tenantId, ct) <= 1;
}
