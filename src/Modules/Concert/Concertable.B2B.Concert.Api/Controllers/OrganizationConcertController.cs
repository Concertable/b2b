using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/organization/concert")]
[HasPermission(SharedPermissions.OperationsView)]
internal sealed class OrganizationConcertController : ControllerBase
{
    private readonly IConcertService concertService;

    public OrganizationConcertController(IConcertService concertService)
    {
        this.concertService = concertService;
    }

    [HttpGet("{concertId:int}")]
    public async Task<ActionResult<MyDetailsResponse>> Get(
        int concertId,
        CancellationToken ct) =>
        (await concertService.GetDetailsForActiveTenantAsync(concertId, ct))
            .ToOkOrProblem(concert => concert.ToMyDetailsResponse());
}
