using Concertable.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class DevController : ControllerBase
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
