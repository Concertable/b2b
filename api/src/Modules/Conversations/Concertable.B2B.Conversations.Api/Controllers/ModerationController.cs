using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.Contracts;
using Concertable.B2B.Admin.Api.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Conversations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Admin]
internal sealed class ModerationController : ControllerBase
{
    private readonly IModerationService moderationService;

    public ModerationController(IModerationService moderationService)
    {
        this.moderationService = moderationService;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<IPagination<ContentReportDto>>> GetReports([FromQuery] PageParams pageParams) =>
        Ok(await moderationService.GetQueueAsync(pageParams));

    [HttpPost("messages/{id}/hide")]
    public async Task<ActionResult> HideMessage(int id) =>
        (await moderationService.HideMessageAsync(id)).ToNoContentOrProblem();

    [HttpPost("messages/{id}/restore")]
    public async Task<ActionResult> RestoreMessage(int id) =>
        (await moderationService.RestoreMessageAsync(id)).ToNoContentOrProblem();

    [HttpPost("reports/{id}/resolve")]
    public async Task<ActionResult> ResolveReport(int id, [FromBody] ResolveReportRequest request) =>
        (await moderationService.ResolveReportAsync(id, request)).ToNoContentOrProblem();
}
