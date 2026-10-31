using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

[ApiController]
[Route("api/organization")]
internal sealed class OrganizationRoleController(IRoleService roles) : ControllerBase
{
    [HttpGet("roles")]
    [HasPermission(TenantPermission.MembersInvite)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles() =>
        (await roles.ListAsync()).ToOkOrProblem();

    [HttpGet("permissions")]
    [HasPermission(TenantPermission.MembersManageRoles)]
    public async Task<ActionResult<IReadOnlyList<PermissionMetadataDto>>> GetPermissions() =>
        (await roles.GetPermissionsAsync()).ToOkOrProblem();

    [HttpPost("roles")]
    [HasPermission(TenantPermission.MembersManageRoles)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request) =>
        (await roles.CreateAsync(request)).ToCreatedOrProblem(
            role => $"/api/organization/roles/{role.Id}");

    [HttpPut("roles/{roleId:guid}")]
    [HasPermission(TenantPermission.MembersManageRoles)]
    public async Task<ActionResult<RoleDto>> Update(Guid roleId, UpdateRoleRequest request) =>
        (await roles.UpdateAsync(roleId, request)).ToOkOrProblem();

    [HttpPost("roles/{roleId:guid}/retire")]
    [HasPermission(TenantPermission.MembersManageRoles)]
    public async Task<IActionResult> Retire(Guid roleId, RetireRoleRequest request) =>
        (await roles.RetireAsync(roleId, request)).ToNoContentOrProblem();
}
