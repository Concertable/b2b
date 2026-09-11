using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Application.Api.Controllers;

[ApiController]
[Route("api/dev")]
internal sealed class ApplicationDevController : ControllerBase
{
    [Authorize]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        [FromQuery] int applicationId,
        [FromServices] IApplicationService applications)
    {
        return (await applications.AcceptAsync(
                applicationId,
                new ESignatureRequest { SignatoryName = "Dev Venue Manager" }))
            .ToNoContentOrProblem();
    }
}
