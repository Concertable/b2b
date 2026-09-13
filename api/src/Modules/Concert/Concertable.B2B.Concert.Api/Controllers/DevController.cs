using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
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
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromQuery] int concertId,
        [FromServices] IConcertWorkflow workflow)
    {
        return (await workflow.CompleteAsync(concertId))
            .Bind(_ => UnitResult.Success<FinishConcertError>())
            .ToNoContentOrProblem();
    }
}
