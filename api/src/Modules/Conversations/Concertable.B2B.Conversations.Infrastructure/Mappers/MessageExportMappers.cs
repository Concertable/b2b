namespace Concertable.B2B.Conversations.Infrastructure.Mappers;

internal static class MessageExportMappers
{
    extension(MessageEntity message)
    {
        public MessageExport ToMessageExport() => new()
        {
            Content = message.Content,
            SenderTenantId = message.SenderTenantId,
            SentDate = message.SentDate,
        };
    }
}
