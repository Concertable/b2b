using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Exceptions;
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

    public async Task ChangeRoleAsync(Guid userId, ChangeMemberRoleRequest request, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var membership = await repository.FindMembershipAsync(tenantId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of tenant {tenantId}.");

        if (membership.Role == TenantRole.Owner && request.Role != TenantRole.Owner)
            await EnsureNotLastOwnerAsync(tenantId, ct);

        membership.ChangeRole(request.Role);
        await repository.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var membership = await repository.FindMembershipAsync(tenantId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of tenant {tenantId}.");

        if (membership.Role == TenantRole.Owner)
            await EnsureNotLastOwnerAsync(tenantId, ct);

        repository.RemoveMembership(membership);
        await repository.SaveChangesAsync(ct);
    }

    public async Task DeleteCurrentTenantAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new NotFoundException($"Tenant {tenantId} not found.");

        foreach (var membership in await repository.ListMembershipsByTenantAsync(tenantId, ct))
            repository.RemoveMembership(membership);

        repository.Remove(tenant);
        await repository.SaveChangesAsync(ct);
    }

    // A tenant must always keep at least one Owner — only Owner holds manage-roles/remove/delete, so an ownerless tenant is unrecoverable.
    private async Task EnsureNotLastOwnerAsync(Guid tenantId, CancellationToken ct)
    {
        if (await repository.CountOwnersAsync(tenantId, ct) <= 1)
            throw new ConflictException("The last owner of an organization cannot be removed or demoted.");
    }
}
