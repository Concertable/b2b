using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Mappers;

internal static class MessageMappers
{
    public static MessageDto ToDto(this MessageEntity message, MessageSender sender, Guid counterpartTenantId) => new()
    {
        Id = message.Id,
        CounterpartTenantId = counterpartTenantId,
        Content = message.Content,
        Sender = sender,
        Action = message.Action
    };
}
