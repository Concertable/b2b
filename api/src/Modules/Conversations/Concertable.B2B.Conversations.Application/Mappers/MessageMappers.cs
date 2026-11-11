using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Mappers;

internal static class MessageMappers
{
    extension(MessageEntity message)
    {
        public MessageDto ToMessageDto(bool canReport = false) => new(
            message.Id,
            message.ConversationId,
            message.Sequence,
            message.SenderTenantId,
            message.SentByUserId,
            message.Content,
            message.SentAt,
            message.Action,
            canReport);
    }
}
