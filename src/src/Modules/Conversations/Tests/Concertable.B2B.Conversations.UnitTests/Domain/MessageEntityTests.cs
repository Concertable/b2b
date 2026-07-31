namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class MessageEntityTests
{
    [Fact]
    public void Create_StampsThePairSenderAndAuthor()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var sentByUserId = Guid.NewGuid();

        var message = MessageEntity.Create(venueTenantId, artistTenantId, senderTenantId: venueTenantId, sentByUserId,
            "content", new DateTime(2026, 1, 1), MessageAction.ApplicationAccepted);

        Assert.Equal(venueTenantId, message.VenueTenantId);
        Assert.Equal(artistTenantId, message.ArtistTenantId);
        Assert.Equal(venueTenantId, message.SenderTenantId);
        Assert.Equal(sentByUserId, message.SentByUserId);
        Assert.Equal(MessageAction.ApplicationAccepted, message.Action);
    }
}
