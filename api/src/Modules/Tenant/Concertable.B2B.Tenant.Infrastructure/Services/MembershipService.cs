using Concertable.B2B.Tenant.Infrastructure.Authorization;
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
    private readonly IRoleRepository roles;
    private readonly IAuthorizationContext authorizationContext;
    private readonly IMembershipReadRepository memberships;
    private readonly IUserModule userModule;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly TenantAuthorizer authorizer;

    public MembershipService(
        IMembershipRepository repository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IRoleRepository roles,
        IAuthorizationContext authorizationContext,
        IMembershipReadRepository memberships,
        IUserModule userModule,
        IOutboxUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider,
        TenantAuthorizer authorizer)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.tenantContext = tenantContext;
        this.roles = roles;
        this.authorizationContext = authorizationContext;
        this.memberships = memberships;
        this.userModule = userModule;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.authorizer = authorizer;
    }

    public Task<Result<IReadOnlyList<MemberDto>, ListMembersError>> ListMembersAsync(
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var tenantId = tenantContext.GetTenantId();
            if (!(await tenantRepository.GetExistingIdsForShareAsync([tenantId], ct)).Contains(tenantId)
                || await GetCurrentActorAsync(tenantId, ct) is not { } actor
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

    public Task<UnitResult<ChangeMemberRolesError>> ChangeRolesAsync(
        Guid userId,
        ChangeMemberRolesRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => ChangeRolesCoreAsync(userId, request, ct), ct);

    private async Task<UnitResult<ChangeMemberRolesError>> ChangeRolesCoreAsync(
        Guid userId,
        ChangeMemberRolesRequest request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new ChangeMemberRolesError.NotPermitted();
        var authorization = await authorizer.AuthorizeOwnerAsync(
            tenantId, TenantPermission.MembersManageRoles, ct);
        if (!authorization.TryGetValue(out var actor))
            return new ChangeMemberRolesError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new ChangeMemberRolesError.MemberNotFound(userId);
        if (!await repository.TryLockUnchangedAsync([member], ct))
            return new ChangeMemberRolesError.NotPermitted();
        var selectedRoles = await roles.ResolveActiveAsync(tenantId, request.RoleIds, ct);
        if (selectedRoles is null)
            return new ChangeMemberRolesError.InvalidRoles();

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

    public Task<UnitResult<RemoveMemberError>> RemoveMemberAsync(
        Guid userId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RemoveMemberCoreAsync(userId, ct), ct);

    private async Task<UnitResult<RemoveMemberError>> RemoveMemberCoreAsync(
        Guid userId,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new RemoveMemberError.NotPermitted();
        if ((await authorizer.AuthorizeOwnerAsync(
                tenantId, TenantPermission.MembersRemove, ct)).IsFailure)
            return new RemoveMemberError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new RemoveMemberError.MemberNotFound(userId);
        if (!await repository.TryLockUnchangedAsync([member], ct))
            return new RemoveMemberError.NotPermitted();

        await repository.SaveChangesAsync(ct);
        var isOwner = (await roles.GetSummariesForMembershipAsync(member.TenantId, member.Id, ct))
            .Any(role => role.IsProtectedOwner);
        if (isOwner && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new RemoveMemberError.LastOwner();

        repository.Remove(member);
        return new Success();
    }

    private async Task<MembershipSnapshot?> GetCurrentActorAsync(
        Guid tenantId, CancellationToken ct)
    {
        var expected = authorizationContext.Membership;
        if (expected is null || expected.TenantId != tenantId)
            return null;
        return await memberships.LockIfUnchangedAsync(expected, ct);
    }
}
