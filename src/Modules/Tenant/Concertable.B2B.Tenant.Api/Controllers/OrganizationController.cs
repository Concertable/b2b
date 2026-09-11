using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Reunion.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organization")]
internal sealed class OrganizationController : ControllerBase
{
    private readonly ITenantService tenantService;

    public OrganizationController(ITenantService tenantService)
    {
        this.tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantDetails>> Get(CancellationToken ct)
    {
        var tenant = await tenantService.GetDetailsAsync(ct);
        return tenant.Match<ActionResult<TenantDetails>>(
            value => Ok(value),
            () => NoContent());
    }

    [HttpPut]
    [HasPermission(SharedPermissions.TenantSettingsEdit)]
    public async Task<ActionResult<TenantDetails>> Update(
        UpdateTenantRequest request,
        CancellationToken ct) =>
        (await tenantService.UpdateAsync(request, ct)).ToOkOrProblem();

    [HttpDelete]
    [HasPermission(SharedPermissions.TenantDelete)]
    public async Task<IActionResult> Delete(CancellationToken ct) =>
        (await tenantService.DeleteAsync(ct)).ToNoContentOrProblem();
}
