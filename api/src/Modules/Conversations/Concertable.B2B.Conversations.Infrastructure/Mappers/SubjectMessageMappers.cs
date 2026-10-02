namespace Concertable.B2B.Conversations.Infrastructure.Mappers;

internal static class SubjectMessageMappers
{
    extension(MessageEntity message)
    {
        public SubjectMessageDto ToSubjectMessageDto() => new()
        {
            Content = message.Content,
            SenderTenantId = message.SenderTenantId,
            SentDate = message.SentDate,
        };
    }
}
