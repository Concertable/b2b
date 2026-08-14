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

    [Fact]
    public void Hide_StampsTheModeratorAndTime_AndRestoreClearsThem()
    {
        var message = MessageEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "content", new DateTime(2026, 1, 1));
        var hiddenByUserId = Guid.NewGuid();
        var hiddenAt = new DateTime(2026, 8, 15, 12, 0, 0);

        message.Hide(hiddenByUserId, hiddenAt);

        Assert.Equal(hiddenAt, message.HiddenAt);
        Assert.Equal(hiddenByUserId, message.HiddenByUserId);

        message.Restore();

        Assert.Null(message.HiddenAt);
        Assert.Null(message.HiddenByUserId);
    }

    [Fact]
    public void Hide_KeepsTheContent()
    {
        var message = MessageEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "the reported content", new DateTime(2026, 1, 1));

        message.Hide(Guid.NewGuid(), new DateTime(2026, 8, 15));

        Assert.Equal("the reported content", message.Content);
    }
}
