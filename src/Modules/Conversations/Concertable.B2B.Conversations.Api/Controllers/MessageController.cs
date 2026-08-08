using Concertable.Contracts;
using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Conversations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission(SharedPermissions.MessagesRead)]
internal sealed class MessageController : ControllerBase
{
    private readonly IMessageService messageService;

    public MessageController(IMessageService messageService)
    {
        this.messageService = messageService;
    }

    [HttpGet("user")]
    public async Task<ActionResult<IPagination<MessageDto>>> GetForUser([FromQuery] PageParams pageParams) =>
        Ok(await messageService.GetInboxAsync(pageParams));

    [HttpGet("user/unread-count")]
    public async Task<ActionResult<int>> GetUnreadCountForUser() =>
        Ok(await messageService.GetUnreadCountForUserAsync());

    [HttpPost("mark-read")]
    public async Task<ActionResult<int>> MarkInboxRead()
    {
        await messageService.MarkInboxReadAsync();
        return Ok(await messageService.GetUnreadCountForUserAsync());
    }
}
