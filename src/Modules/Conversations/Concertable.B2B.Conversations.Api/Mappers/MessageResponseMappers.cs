using Concertable.B2B.Conversations.Api.Responses;
using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Conversations.Api.Mappers;

internal static class MessageResponseMappers
{
    public static IPagination<MessageResponse> ToResponses(this IPagination<MessageDto> messages) =>
        messages.Map(m => m.ToResponse());

    public static MessageResponse ToResponse(this MessageDto message) => new()
    {
        Id = message.Id,
        CounterpartTenantId = message.CounterpartTenantId,
        Sender = message.Sender,
        Action = message.Action,
        Content = message.Content,
        Actions = new MessageActions(ReportLink(message))
    };

    // You cannot report your own tenant's message, and the sender kind already answers that.
    private static ActionLink? ReportLink(MessageDto message) =>
        message.Sender.Kind == MessageSenderKind.Org
            ? new ActionLink($"/api/Message/{message.Id}/report", HttpMethods.Post)
            : null;
}
