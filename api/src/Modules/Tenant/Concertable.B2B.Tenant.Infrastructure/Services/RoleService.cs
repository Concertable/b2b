using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class RoleService : IRoleService
{
    private readonly IRoleRepository roles;
    private readonly ITenantRepository tenants;
    private readonly IMembershipRepository memberships;
    private readonly ITenantContext tenantContext;
    private readonly IMembershipContext membershipContext;
    private readonly IMembershipResolver membershipResolver;
    private readonly IInvitationRepository invitations;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly TenantAuthorityResolver authority;
    private readonly IAuthorizationContext authorizationContext;

    public RoleService(
        IRoleRepository roles,
        ITenantRepository tenants,
        IMembershipRepository memberships,
        ITenantContext tenantContext,
        IMembershipContext membershipContext,
        IMembershipResolver membershipResolver,
        IInvitationRepository invitations,
        IOutboxUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider,
        TenantAuthorityResolver authority,
        IAuthorizationContext authorizationContext)
    {
        this.roles = roles;
        this.tenants = tenants;
        this.memberships = memberships;
        this.tenantContext = tenantContext;
        this.membershipContext = membershipContext;
        this.membershipResolver = membershipResolver;
        this.invitations = invitations;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.authority = authority;
        this.authorizationContext = authorizationContext;
    }

    public Task<Result<IReadOnlyList<RoleDto>, ListRolesError>> ListAsync(CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var tenantId = tenantContext.GetTenantId();
            if (!(await tenants.GetExistingIdsForShareAsync([tenantId], ct)).Contains(tenantId))
                return Result.Failure<IReadOnlyList<RoleDto>, ListRolesError>(
                    new ListRolesError.NotPermitted());
            var actor = await GetCurrentActorAsync(tenantId, ct);
            if (actor is null || !actor.HasPermission(TenantPermission.MembersInvite))
                return Result.Failure<IReadOnlyList<RoleDto>, ListRolesError>(
                    new ListRolesError.NotPermitted());

            var available = await roles.ListActiveAsync(tenantId, ct);
            var isOwner = await roles.HasProtectedOwnerAsync(actor.TenantId, actor.MembershipId, ct);
            var visible = isOwner ? available : available
                .Where(role => TenantRoleAssignmentPolicy.CanAssign(actor, false, [role])).ToList();
            return Result.Success<IReadOnlyList<RoleDto>, ListRolesError>(
                visible.Select(ToDto).ToList());
        }, ct);

    public Task<Result<IReadOnlyList<PermissionMetadataDto>, ListRolesError>> GetPermissionsAsync(
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var tenantId = tenantContext.GetTenantId();
            if (!(await tenants.GetExistingIdsForShareAsync([tenantId], ct)).Contains(tenantId)
                || await GetCurrentActorAsync(tenantId, ct) is not { } actor
                || !await roles.HasProtectedOwnerAsync(actor.TenantId, actor.MembershipId, ct))
                return Result.Failure<IReadOnlyList<PermissionMetadataDto>, ListRolesError>(
                    new ListRolesError.NotPermitted());

            IReadOnlyList<PermissionMetadataDto> metadata = AuthorizationCatalog.Permissions.Values
                .OrderBy(item => item.Category).ThenBy(item => item.Label)
                .Select(item => new PermissionMetadataDto(
                    item.Permission.Value, item.Label, item.Category,
                    [.. item.ResourceBindings.Select(binding => new ResourceBinding(
                        binding.Resource, binding.Facet, binding.Policy, binding.RequiresScopes))],
                    [.. item.AssignableAudiences.Select(audience => audience.ToString())],
                    item.OwnerOnly))
                .ToArray();
            return Result.Success<IReadOnlyList<PermissionMetadataDto>, ListRolesError>(metadata);
        }, ct);

    public Task<Result<RoleDto, CreateRoleError>> CreateAsync(
        CreateRoleRequest request, CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => CreateCoreAsync(request, ct), ct);

    private async Task<Result<RoleDto, CreateRoleError>> CreateCoreAsync(
        CreateRoleRequest request, CancellationToken ct)
    {
        authorizationContext.RegisterFailure<Result<RoleDto, CreateRoleError>>(
            () => Result.Failure<RoleDto, CreateRoleError>(new CreateRoleError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenants.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null || await authority.AuthorizeAdministrationAsync(
            tenantId, TenantPermission.MembersManageRoles, true, ct) is null)
            return new CreateRoleError.NotPermitted();
        if (!TryParseGrants(request.Permissions, out var grants)
            || !ValidName(request.Name))
            return new CreateRoleError.Invalid();
        var name = request.Name.Trim();
        if (await roles.NameExistsAsync(tenantId, name, null, ct))
            return new CreateRoleError.NameInUse();

        var role = TenantRoleDefinition.CreateCustom(
            tenantId, name, request.IsInvitationAssignable, grants);
        roles.Insert(role);
        var beforeVersion = tenant.RolePolicyVersion;
        tenant.AdvanceRolePolicyVersion();
        authority.RecordPolicyVersion(tenant, beforeVersion);
        return ToDto(role);
    }

    public Task<Result<RoleDto, UpdateRoleError>> UpdateAsync(
        Guid roleId, UpdateRoleRequest request, CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => UpdateCoreAsync(roleId, request, ct), ct);

    private async Task<Result<RoleDto, UpdateRoleError>> UpdateCoreAsync(
        Guid roleId, UpdateRoleRequest request, CancellationToken ct)
    {
        authorizationContext.RegisterFailure<Result<RoleDto, UpdateRoleError>>(
            () => Result.Failure<RoleDto, UpdateRoleError>(new UpdateRoleError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenants.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null || await authority.AuthorizeAdministrationAsync(
            tenantId, TenantPermission.MembersManageRoles, true, ct) is null)
            return new UpdateRoleError.NotPermitted();
        var role = await roles.GetActiveByIdAsync(tenantId, roleId, ct);
        if (role is null)
            return new UpdateRoleError.NotFound();
        if (role.SystemPresetKey is not null || role.IsProtectedOwner)
            return new UpdateRoleError.Protected();
        if (role.Version != request.ExpectedVersion)
            return new UpdateRoleError.Superseded();
        if (!TryParseGrants(request.Permissions, out var grants) || !ValidName(request.Name))
            return new UpdateRoleError.Invalid();
        var name = request.Name.Trim();
        if (await roles.NameExistsAsync(tenantId, name, roleId, ct))
            return new UpdateRoleError.NameInUse();

        role.Update(name, request.IsInvitationAssignable, grants);
        var beforeVersion = tenant.RolePolicyVersion;
        tenant.AdvanceRolePolicyVersion();
        authority.RecordPolicyVersion(tenant, beforeVersion);
        return ToDto(role);
    }

    public Task<UnitResult<RetireRoleError>> RetireAsync(
        Guid roleId, RetireRoleRequest request, CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RetireCoreAsync(roleId, request, ct), ct);

    private async Task<UnitResult<RetireRoleError>> RetireCoreAsync(
        Guid roleId, RetireRoleRequest request, CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<RetireRoleError>>(
            () => UnitResult.Failure<RetireRoleError>(new RetireRoleError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenants.GetByIdForAdministrationAsync(tenantId, ct);
        var actor = tenant is null ? null : await authority.AuthorizeAdministrationAsync(
            tenantId, TenantPermission.MembersManageRoles, true, ct);
        if (tenant is null || actor is null)
            return new RetireRoleError.NotPermitted();
        var role = await roles.GetActiveByIdAsync(tenantId, roleId, ct);
        if (role is null)
            return new RetireRoleError.NotFound();
        if (role.SystemPresetKey is not null || role.IsProtectedOwner)
            return new RetireRoleError.Protected();
        if (role.Version != request.ExpectedVersion)
            return new RetireRoleError.Superseded();
        var affected = await memberships.ListAssignedToRoleAsync(tenantId, roleId, ct);
        TenantRoleDefinition? replacement = null;
        if (request.ReplacementRoleId is { } replacementId)
        {
            if (replacementId == roleId)
                return new RetireRoleError.InvalidReplacement();
            replacement = await roles.GetActiveByIdAsync(tenantId, replacementId, ct);
            if (replacement is null)
                return new RetireRoleError.InvalidReplacement();
        }
        if (affected.Count > 0 && replacement is null)
            return new RetireRoleError.ReplacementRequired();

        foreach (var member in affected.OrderBy(member => member.Id))
        {
            if (!await authority.TrackMembershipAsync(member, ct))
                return new RetireRoleError.Superseded();
        }
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var member in affected)
        {
            var roleIds = member.Assignments.Where(row => row.RoleId != roleId)
                .Select(row => row.RoleId).Append(replacement!.Id).Distinct().ToArray();
            var beforeVersion = member.PermissionVersion;
            member.ReplaceRoles(roleIds, actor.MembershipId, now);
            if (member.PermissionVersion != beforeVersion)
                authority.RecordMembershipVersion(member, beforeVersion);
        }
        var pendingInvitations = await invitations.ListPendingAssignedToRoleAsync(
            tenantId, roleId, ct);
        foreach (var invitation in pendingInvitations)
            invitation.Revoke();
        role.Retire(now);
        var beforePolicyVersion = tenant.RolePolicyVersion;
        tenant.AdvanceRolePolicyVersion();
        authority.RecordPolicyVersion(tenant, beforePolicyVersion);
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

    private static bool ValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 100;

    private static bool TryParseGrants(
        IReadOnlyList<RolePermissionDto> items,
        out IReadOnlyDictionary<TenantPermission, ResourceAudience> grants)
    {
        var parsed = new Dictionary<TenantPermission, ResourceAudience>();
        foreach (var item in items)
        {
            if (!TenantPermission.TryParse(item.Permission, out var permission)
                || !Enum.TryParse<ResourceAudience>(item.Audience, false, out var audience)
                || audience == ResourceAudience.None
                || !AuthorizationCatalog.Permissions.TryGetValue(permission, out var descriptor)
                || descriptor.OwnerOnly
                || !descriptor.AssignableAudiences.Contains(audience)
                || !parsed.TryAdd(permission, audience))
            {
                grants = parsed;
                return false;
            }
        }
        grants = parsed;
        return true;
    }

    private static RoleDto ToDto(TenantRoleDefinition role) =>
        new(role.Id, role.Name, role.Version, role.SystemPresetKey is not null,
            role.IsProtectedOwner, role.IsInvitationAssignable,
            [.. role.Permissions.OrderBy(row => row.PermissionKey, StringComparer.Ordinal)
                .Select(row => new RolePermissionDto(row.PermissionKey, row.Audience.ToString()))]);
}
