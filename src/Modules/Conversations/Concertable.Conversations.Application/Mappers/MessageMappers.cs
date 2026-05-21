using Concertable.Conversations.Application.DTOs;

namespace Concertable.Conversations.Application.Mappers;

internal static class MessageMappers
{
    public static MessageDto ToDto(this MessageEntity message, MessageUserDto fromUser) => new()
    {
        Id = message.Id,
        Content = message.Content,
        FromUser = fromUser,
        Action = message.Action
    };

    public static MessageUserDto ToMessageUserDto(this IUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Latitude = user.Latitude,
        Longitude = user.Longitude,
        County = user.County,
        Town = user.Town
    };
}
