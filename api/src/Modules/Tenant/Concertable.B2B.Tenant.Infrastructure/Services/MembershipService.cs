using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class MembershipService : IMembershipService
{
    private readonly IMembershipRepository repository;
    private readonly ITenantRepository tenantRepository;
    private readonly ITenantContext tenantContext;
    private readonly IRoleRepository roles;
    private readonly IAuthorizationContext authorizationContext;
    private readonly IUserModule userModule;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;

    public MembershipService(
        IMembershipRepository repository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IRoleRepository roles,
        IAuthorizationContext authorizationContext,
        IUserModule userModule,
        IOutboxUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.tenantContext = tenantContext;
        this.roles = roles;
        this.authorizationContext = authorizationContext;
        this.userModule = userModule;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public Task<Result<IReadOnlyList<MemberDto>, ListMembersError>> ListMembersAsync(
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var tenantId = tenantContext.GetTenantId();
            if (!await tenantRepository.ExistsByIdWithKeyShareLockAsync(tenantId, ct)
                || !authorizationContext.TryGetMembership(tenantId, out var actor)
                || !actor.HasPermission(TenantPermission.OperationsView))
                return Result.Failure<IReadOnlyList<MemberDto>, ListMembersError>(
                    new ListMembersError.NotPermitted());
            var memberships = await repository.ListMembershipsByTenantAsync(tenantId, ct);
            var emails = await userModule.GetEmailsByIdsAsync(memberships.Select(member => member.UserId));
            var summaries = await roles.GetSummariesForMembershipsAsync(
                tenantId, memberships.Select(member => member.Id).ToArray(), ct);
            var result = new List<MemberDto>(memberships.Count);
            foreach (var member in memberships)
                result.Add(new MemberDto(member.UserId, emails[member.UserId],
                    summaries.GetValueOrDefault(member.Id) ?? Array.Empty<RoleSummary>()));
            return Result.Success<IReadOnlyList<MemberDto>, ListMembersError>(result);
        }, ct);

    public async Task<UnitResult<ChangeMemberRolesError>> ChangeRolesAsync(
        Guid userId,
        ChangeMemberRolesRequest request,
        CancellationToken ct = default)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(() => ChangeRolesCoreAsync(userId, request, ct), ct);
        }
        catch (DbUpdateException exception) when (exception.IsMembershipConcurrencyConflict())
        {
            return new ChangeMemberRolesError.NotPermitted();
        }
    }

    private async Task<UnitResult<ChangeMemberRolesError>> ChangeRolesCoreAsync(
        Guid userId,
        ChangeMemberRolesRequest request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenantRepository.GetByIdWithUpdateLockAsync(tenantId, ct);
        if (tenant is null)
            return new ChangeMemberRolesError.NotPermitted();
        if (!authorizationContext.TryGetMembership(tenantId, out var actor)
            || !actor.HasPermission(TenantPermission.MembersManageRoles)
            || !await roles.HasProtectedOwnerAsync(tenantId, actor.MembershipId, ct))
            return new ChangeMemberRolesError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new ChangeMemberRolesError.MemberNotFound(userId);
        var selectedRoles = await roles.ResolveActiveAsync(tenantId, request.RoleIds, ct);
        if (selectedRoles is null)
            return new ChangeMemberRolesError.InvalidRoles();
        if (!TenantRoleAssignmentPolicy.AreAvailableForTenant(tenant, selectedRoles))
            return new ChangeMemberRolesError.RoleNotAvailableForTenant();

        await repository.SaveChangesAsync(ct);
        var wasOwner = (await roles.GetSummariesForMembershipAsync(member.TenantId, member.Id, ct))
            .Any(role => role.IsProtectedOwner);
        if (wasOwner && selectedRoles.All(role => !role.IsProtectedOwner)
            && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new ChangeMemberRolesError.LastOwner();

        return member.ChangeRoles(
                request.RoleIds.ToHashSet(), actor.MembershipId, timeProvider.GetUtcNow().UtcDateTime)
            .MapError(error => error.ToChangeMemberRolesError());
    }

    public async Task<UnitResult<RemoveMemberError>> RemoveMemberAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(() => RemoveMemberCoreAsync(userId, ct), ct);
        }
        catch (DbUpdateException exception) when (exception.IsMembershipConcurrencyConflict())
        {
            return new RemoveMemberError.NotPermitted();
        }
    }

    private async Task<UnitResult<RemoveMemberError>> RemoveMemberCoreAsync(
        Guid userId,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdWithUpdateLockAsync(tenantId, ct) is null)
            return new RemoveMemberError.NotPermitted();
        if (!authorizationContext.TryGetMembership(tenantId, out var actor)
            || !actor.HasPermission(TenantPermission.MembersRemove)
            || !await roles.HasProtectedOwnerAsync(tenantId, actor.MembershipId, ct))
            return new RemoveMemberError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new RemoveMemberError.MemberNotFound(userId);

        await repository.SaveChangesAsync(ct);
        var isOwner = (await roles.GetSummariesForMembershipAsync(member.TenantId, member.Id, ct))
            .Any(role => role.IsProtectedOwner);
        if (isOwner && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new RemoveMemberError.LastOwner();

        repository.Remove(member);
        return new Success();
    }

}
