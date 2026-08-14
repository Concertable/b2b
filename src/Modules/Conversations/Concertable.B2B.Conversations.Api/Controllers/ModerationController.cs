using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.User.Api.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Conversations.Api.Controllers;

/// <summary>
/// Platform moderation of reported content. Gated on <see cref="AdminAttribute"/>, never on tenant RBAC:
/// a tenant role is scoped to one tenant, and an operator acting on someone else's thread is a platform
/// question. There is no admin SPA yet, so these are driven from Swagger/curl.
/// </summary>
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
    public async Task<ActionResult<IReadOnlyList<ContentReportDto>>> GetReports() =>
        Ok(await moderationService.GetQueueAsync());

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
