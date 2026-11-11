using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IRoleService
{
    Task<Result<IReadOnlyList<RoleDto>, ListRolesError>> ListAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<PermissionMetadataDto>, ListRolesError>> GetPermissionsAsync(CancellationToken ct = default);
    Task<Result<RoleDto, CreateRoleError>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<Result<RoleDto, UpdateRoleError>> UpdateAsync(Guid roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<UnitResult<RetireRoleError>> RetireAsync(Guid roleId, RetireRoleRequest request, CancellationToken ct = default);
}
