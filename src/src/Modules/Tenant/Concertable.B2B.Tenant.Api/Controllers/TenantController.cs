using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Reunion.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// The user-facing surface of the tenant — "Organization" in UI/API vocabulary. The caller's own tenant is
/// resolved from <c>ITenantContext</c>; a principal without a tenant gets nothing (GET) or 403 (PUT/DELETE).
/// Deleting the tenant is the only action gated by an explicit permission.
/// </summary>
[ApiController]
[Authorize]
[Route("api/organizations")]
internal sealed class TenantController : ControllerBase
{
    private readonly ITenantService tenantService;

    public TenantController(ITenantService tenantService)
    {
        this.tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantDetails>> GetForCurrentUser()
    {
        var tenant = await tenantService.GetDetailsForCurrentTenantAsync();
        return tenant.Match<ActionResult<TenantDetails>>(
            value => Ok(value),
            () => NoContent());
    }

    [HttpPut]
    public async Task<ActionResult<TenantDetails>> Update(UpdateTenantRequest request) =>
        (await tenantService.UpdateAsync(request)).ToOkOrProblem();

    [HttpDelete]
    [HasPermission(SharedPermissions.TenantDelete)]
    public async Task<IActionResult> DeleteCurrentTenant() =>
        (await tenantService.DeleteCurrentTenantAsync()).ToNoContentOrProblem();
}
