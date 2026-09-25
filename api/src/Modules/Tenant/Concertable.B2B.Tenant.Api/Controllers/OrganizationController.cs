using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
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

    [HttpPost]
    public async Task<ActionResult<TenantDetails>> Create(
        CreateTenantRequest request,
        CancellationToken ct) =>
        (await tenantService.CreateAsync(request, ct)).ToCreatedOrProblem(tenant => $"/api/organization/{tenant.Id}");

    [HttpPut]
    [HasPermission(TenantPermission.TenantSettingsEditName)]
    public async Task<ActionResult<TenantDetails>> Update(
        UpdateTenantRequest request,
        CancellationToken ct) =>
        (await tenantService.UpdateAsync(request, ct)).ToOkOrProblem();

    [HttpPut("activities/{kind}")]
    [HasPermission(TenantPermission.TenantSettingsEditName)]
    public async Task<ActionResult<TenantDetails>> ActivateActivity(
        TenantBusinessActivityKind kind,
        ChangeBusinessActivityRequest request,
        CancellationToken ct) =>
        (await tenantService.ActivateBusinessActivityAsync(kind, request, ct)).ToOkOrProblem();

    [HttpDelete("activities/{kind}")]
    [HasPermission(TenantPermission.TenantSettingsEditName)]
    public async Task<ActionResult<TenantDetails>> RetireActivity(
        TenantBusinessActivityKind kind,
        [FromBody] ChangeBusinessActivityRequest request,
        CancellationToken ct) =>
        (await tenantService.RetireBusinessActivityAsync(kind, request, ct)).ToOkOrProblem();

    [HttpDelete]
    [HasPermission(TenantPermission.TenantDeleteName)]
    public async Task<IActionResult> Delete(CancellationToken ct) =>
        (await tenantService.DeleteAsync(ct)).ToNoContentOrProblem();
}
