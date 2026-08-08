using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class MessageEntity : IIdEntity, IVenueArtistTenantScoped
{
    private MessageEntity() { }

    public int Id { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid SenderTenantId { get; private set; }
    public Guid SentByUserId { get; private set; }
    public MessageAction? Action { get; private set; }
    public DateTime SentDate { get; private set; }

    public static MessageEntity Create(
        Guid venueTenantId,
        Guid artistTenantId,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        DateTime sentDate,
        MessageAction? action = null) => new()
        {
            VenueTenantId = venueTenantId,
            ArtistTenantId = artistTenantId,
            SenderTenantId = senderTenantId,
            SentByUserId = sentByUserId,
            Content = content,
            SentDate = sentDate,
            Action = action
        };
}
