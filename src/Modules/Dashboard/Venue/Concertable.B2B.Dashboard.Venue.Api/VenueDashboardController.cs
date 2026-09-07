using Concertable.B2B.Dashboard.Venue.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Dashboard.Venue.Api;

[ApiController]
[RequiredTenantType(TenantType.Venue)]
[HasPermission(SharedPermissions.OperationsView)]
[Route("api/venue-dashboard")]
internal sealed class VenueDashboardController : ControllerBase
{
    private readonly IVenueDashboardService service;

    public VenueDashboardController(IVenueDashboardService service) =>
        this.service = service;

    [HttpGet("kpis")]
    public async Task<ActionResult<VenueDashboardKpis>> GetKpis(CancellationToken ct) =>
        (await service.GetAsync(ct)).ToOkOrNoContent();

    [HttpGet("overview")]
    public async Task<ActionResult<VenueDashboardOverview>> GetOverview(CancellationToken ct) =>
        (await service.GetOverviewAsync(ct)).ToOkOrNoContent();

    [HttpGet("charts/payment-revenue")]
    public async Task<ActionResult<IReadOnlyList<MonthlyRevenuePoint>>> GetPaymentRevenue(CancellationToken ct) =>
        Ok(await service.GetPaymentRevenueAsync(ct));

    [HttpGet("settlements")]
    public async Task<ActionResult<IReadOnlyList<Settlement>>> GetSettlements(CancellationToken ct) =>
        Ok(await service.GetSettlementsAsync(ct));

    [HttpGet("activity")]
    public async Task<ActionResult<IReadOnlyList<ActivityItemDto>>> GetActivity(CancellationToken ct) =>
        Ok(await service.GetActivityAsync(ct));
}
