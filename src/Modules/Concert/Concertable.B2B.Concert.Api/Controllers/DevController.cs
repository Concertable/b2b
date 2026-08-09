using Concertable.B2B.Concert.Application.Requests;
using Concertable.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

/// <summary>
/// Dev-frontend convenience endpoints for manually driving workflow transitions during local development.
/// MUST NOT be used by tests at any level — tests invoke transitions through the real surface instead:
/// resolve the executor from DI (integration) or drive the production trigger (E2E).
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal sealed class DevController : ControllerBase
{
    [Authorize]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        [FromQuery] int applicationId,
        [FromServices] IAcceptExecutor acceptExecutor)
    {
        return (await acceptExecutor.AcceptAsync(
            applicationId,
            null,
            new ESignatureRequest { SignatoryName = "Dev Venue Manager" }))
            .ToNoContentOrProblem();
    }

    [Authorize]
    [HttpPost("complete")]
    public async Task<ActionResult<SettlementOutcome>> Complete(
        [FromQuery] int concertId,
        [FromServices] IFinishExecutor finishExecutor)
    {
        return (await finishExecutor.FinishAsync(concertId)).ToOkOrProblem();
    }
}
