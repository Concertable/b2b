using Concertable.B2B.Conversations.Api.Responses;
using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Conversations.Api.Mappers;

internal static class MessageMappers
{
    extension(IPagination<MessageDto> messages)
    {
        public IPagination<MessageResponse> ToResponses() => messages.Map(m => m.ToResponse());
    }

    extension(MessageDto message)
    {
        public MessageResponse ToResponse() => new()
        {
            Id = message.Id,
            CounterpartTenantId = message.CounterpartTenantId,
            Sender = message.Sender,
            Action = message.Action,
            Content = message.Content,
            Actions = new MessageActions(ReportLink(message))
        };
    }

    // You cannot report your own tenant's message, and the sender kind already answers that.
    private static ActionLink? ReportLink(MessageDto message) =>
        message.Sender.Kind == MessageSenderKind.Org
            ? new ActionLink($"/api/Message/{message.Id}/report", HttpMethods.Post)
            : null;
}
