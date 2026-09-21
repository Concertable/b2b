using Concertable.B2B.Conversations.Api.Mappers;
using Concertable.B2B.Conversations.Api.Responses;
using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Conversations.Api.Controllers;

[ApiController]
[Route("api/conversations")]
internal sealed class ConversationsController : ControllerBase
{
    private readonly IConversationService conversationService;
    private readonly IContentReportService contentReportService;

    public ConversationsController(
        IConversationService conversationService,
        IContentReportService contentReportService)
    {
        this.conversationService = conversationService;
        this.contentReportService = contentReportService;
    }

    [HasPermission(TenantPermission.MessagesSendName)]
    [HttpPost]
    public async Task<ActionResult<ConversationResponse>> Create(
        [FromBody] CreateConversationRequest request,
        CancellationToken ct) =>
        (await conversationService.CreateAsync(request, ct))
            .Map(conversation => conversation.ToResponse())
            .ToCreatedOrProblem(conversation => $"/api/conversations/{conversation.ConversationId}");

    [HasPermission(TenantPermission.MessagesReadName)]
    [HttpGet("{conversationId:int}")]
    public async Task<ActionResult<ConversationResponse>> Get(
        int conversationId,
        CancellationToken ct) =>
        (await conversationService.GetAsync(conversationId, ct)).ToOkOrProblem(
            conversation => conversation.ToResponse());

    [HasPermission(TenantPermission.MessagesReadName)]
    [HttpGet("{conversationId:int}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetMessages(
        int conversationId,
        CancellationToken ct) =>
        (await conversationService.GetMessagesAsync(conversationId, ct)).ToOkOrProblem(
            messages => (IReadOnlyList<MessageResponse>)messages
                .Select(message => message.ToResponse()).ToList());

    [HasPermission(TenantPermission.MessagesSendName)]
    [EnableRateLimiting(RateLimitPolicies.Messaging)]
    [HttpPost("{conversationId:int}/messages")]
    public async Task<ActionResult<MessageResponse>> Send(
        int conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct) =>
        (await conversationService.SendAsync(conversationId, request, ct: ct))
            .Map(message => message.ToResponse())
            .ToCreatedOrProblem(message => $"/api/conversations/{conversationId}/messages/{message.Id}");

    [HasPermission(TenantPermission.MessagesReadName)]
    [HttpPut("{conversationId:int}/read-position")]
    public async Task<IActionResult> AdvanceReadPosition(
        int conversationId,
        [FromBody] AdvanceConversationReadPositionRequest request,
        CancellationToken ct) =>
        (await conversationService.AdvanceReadPositionAsync(conversationId, request, ct))
            .ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpPost("{conversationId:int}/member-assignments")]
    public async Task<IActionResult> AssignMember(
        int conversationId,
        [FromBody] AssignConversationMemberRequest request,
        CancellationToken ct) =>
        (await conversationService.AssignMemberAsync(conversationId, request, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpDelete("{conversationId:int}/member-assignments/{membershipId:guid}")]
    public async Task<IActionResult> RemoveMemberAssignment(
        int conversationId,
        Guid membershipId,
        [FromQuery, BindRequired] long expectedVersion,
        CancellationToken ct) =>
        (await conversationService.RemoveMemberAssignmentAsync(
            conversationId, membershipId, expectedVersion, ct))
            .ToNoContentOrProblem();

    [HasPermission(TenantPermission.MessagesReadName)]
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct) =>
        Ok(await conversationService.GetUnreadCountAsync(ct));

    [HasPermission(TenantPermission.MessagesReadName)]
    [HttpGet("previews")]
    public async Task<ActionResult<IReadOnlyList<MessagePreviewDto>>> GetRecentPreviews(CancellationToken ct) =>
        Ok(await conversationService.GetRecentPreviewsAsync(ct));

    [HasPermission(TenantPermission.MessagesReadName)]
    [EnableRateLimiting(RateLimitPolicies.Messaging)]
    [HttpPost("{conversationId:int}/messages/{messageId:int}/report")]
    public async Task<ActionResult> Report(
        int conversationId,
        int messageId,
        [FromBody] ReportMessageRequest request) =>
        (await contentReportService.SubmitAsync(conversationId, messageId, request)).ToNoContentOrProblem();
}
