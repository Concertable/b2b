using Concertable.Contracts;
using Concertable.B2B.Conversations.Api.Mappers;
using Concertable.B2B.Conversations.Api.Responses;
using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Conversations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission(SharedPermissions.MessagesRead)]
internal sealed class MessageController : ControllerBase
{
    private readonly IMessageService messageService;
    private readonly IContentReportService contentReportService;

    public MessageController(IMessageService messageService, IContentReportService contentReportService)
    {
        this.messageService = messageService;
        this.contentReportService = contentReportService;
    }

    [HttpGet("user")]
    public async Task<ActionResult<IPagination<MessageResponse>>> GetForUser([FromQuery] PageParams pageParams) =>
        Ok((await messageService.GetInboxAsync(pageParams)).ToResponses());

    [HttpGet("user/unread-count")]
    public async Task<ActionResult<int>> GetUnreadCountForUser() =>
        Ok(await messageService.GetUnreadCountForUserAsync());

    [HttpGet("previews")]
    public async Task<ActionResult<IReadOnlyList<MessagePreviewDto>>> GetRecentPreviews() =>
        Ok(await messageService.GetRecentPreviewsAsync());

    [HttpPost("mark-read")]
    public async Task<ActionResult<int>> MarkInboxRead()
    {
        await messageService.MarkInboxReadAsync();
        return Ok(await messageService.GetUnreadCountForUserAsync());
    }

    [EnableRateLimiting(RateLimitPolicies.Messaging)]
    [HttpPost("{id}/report")]
    public async Task<ActionResult> Report(int id, [FromBody] ReportMessageRequest request) =>
        (await contentReportService.SubmitAsync(id, request)).ToNoContentOrProblem();
}
