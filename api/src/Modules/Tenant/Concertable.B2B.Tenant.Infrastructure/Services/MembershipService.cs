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
    private readonly IMembershipContext membershipContext;
    private readonly IMembershipResolver membershipResolver;
    private readonly IUserModule userModule;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly TenantAuthorityResolver authority;
    private readonly ICommandAuthorizationContext command;

    public MembershipService(
        IMembershipRepository repository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IRoleRepository roles,
        IMembershipContext membershipContext,
        IMembershipResolver membershipResolver,
        IUserModule userModule,
        IOutboxUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider,
        TenantAuthorityResolver authority,
        ICommandAuthorizationContext command)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.tenantContext = tenantContext;
        this.roles = roles;
        this.membershipContext = membershipContext;
        this.membershipResolver = membershipResolver;
        this.userModule = userModule;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.authority = authority;
        this.command = command;
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
                    summaries.GetValueOrDefault(member.Id) ?? Array.Empty<RoleSummaryDto>()));
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
        command.RegisterFailure<UnitResult<ChangeMemberRolesError>>(
            () => UnitResult.Failure<ChangeMemberRolesError>(
                new ChangeMemberRolesError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new ChangeMemberRolesError.NotPermitted();
        var actor = await authority.ProveAdministrationAsync(
            tenantId, TenantPermission.MembersManageRoles, true, ct);
        if (actor is null)
            return new ChangeMemberRolesError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new ChangeMemberRolesError.MemberNotFound(userId);
        if (!await authority.TrackMembershipAsync(member, ct))
            return new ChangeMemberRolesError.NotPermitted();
        var selectedRoles = await roles.ResolveActiveAsync(tenantId, request.RoleIds, ct);
        if (selectedRoles is null)
            return new ChangeMemberRolesError.InvalidRoles();

        var wasOwner = (await roles.GetSummariesForMembershipAsync(member.TenantId, member.Id, ct))
            .Any(role => role.IsProtectedOwner);
        if (wasOwner && selectedRoles.All(role => !role.IsProtectedOwner)
            && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new ChangeMemberRolesError.LastOwner();

        var beforeVersion = member.PermissionVersion;
        member.ReplaceRoles(request.RoleIds, actor.MembershipId, timeProvider.GetUtcNow().UtcDateTime);
        if (member.PermissionVersion != beforeVersion)
            authority.RecordMembershipVersion(member, beforeVersion);
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
        command.RegisterFailure<UnitResult<RemoveMemberError>>(
            () => UnitResult.Failure<RemoveMemberError>(new RemoveMemberError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new RemoveMemberError.NotPermitted();
        var actor = await authority.ProveAdministrationAsync(
            tenantId, TenantPermission.MembersRemove, true, ct);
        if (actor is null)
            return new RemoveMemberError.NotPermitted();

        var member = await repository.FindMembershipAsync(tenantId, userId, ct);
        if (member is null)
            return new RemoveMemberError.MemberNotFound(userId);
        if (!await authority.TrackMembershipAsync(member, ct))
            return new RemoveMemberError.NotPermitted();

        var isOwner = (await roles.GetSummariesForMembershipAsync(member.TenantId, member.Id, ct))
            .Any(role => role.IsProtectedOwner);
        if (isOwner && await repository.CountOwnersAsync(tenantId, ct) <= 1)
            return new RemoveMemberError.LastOwner();

        authority.RecordMembershipRemoval(member);
        repository.Remove(member);
        return new Success();
    }

    private async Task<MembershipSnapshot?> GetCurrentActorAsync(
        Guid tenantId, CancellationToken ct)
    {
        var expected = membershipContext.Membership;
        if (expected is null || expected.TenantId != tenantId)
            return null;
        var resolved = await membershipResolver.ResolveSnapshotAsync(expected, ct);
        return resolved.TryGetValue(out var current) ? current : null;
    }
}
