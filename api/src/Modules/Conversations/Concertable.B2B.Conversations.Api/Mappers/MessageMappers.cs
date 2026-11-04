using Concertable.B2B.Conversations.Api.Responses;
using Concertable.B2B.Conversations.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Conversations.Api.Mappers;

internal static class MessageMappers
{
    public static ConversationResponse ToResponse(this ConversationDto conversation) => new(
        conversation.ConversationId,
        conversation.AccessVersion,
        conversation.Participants);

    public static MessageResponse ToResponse(this MessageDto message) => new(
        message.Id,
        message.ConversationId,
        message.Sequence,
        message.SenderTenantId,
        message.SentByUserId,
        message.Content,
        message.SentAt,
        message.Action,
        new MessageActions(message.CanReport
            ? new ActionLink(
                $"/api/conversations/{message.ConversationId}/messages/{message.Id}/report",
                HttpMethods.Post)
            : null));
}
