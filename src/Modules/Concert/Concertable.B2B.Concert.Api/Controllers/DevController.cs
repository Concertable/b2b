using Concertable.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

/// <summary>
/// Dev-frontend convenience endpoints for manually driving workflow transitions during local development.
/// MUST NOT be used by tests at any level — tests invoke transitions through the real surface instead:
/// resolve <c>IConcertWorkflowModule</c> from DI (integration) or drive the production trigger (E2E).
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal sealed class DevController : ControllerBase
{
    [Authorize]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        [FromQuery] int applicationId,
        [FromServices] IAcceptanceDispatcher AcceptanceDispatcher)
    {
        await AcceptanceDispatcher.AcceptAsync(applicationId, null);
        return NoContent();
    }

    [Authorize]
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromQuery] int concertId,
        [FromServices] ICompletionDispatcher CompletionDispatcher)
    {
        var result = await CompletionDispatcher.FinishAsync(concertId);
        return result.IsFailed
            ? BadRequest(result.Errors.SelectMessages())
            : Ok();
    }
}
