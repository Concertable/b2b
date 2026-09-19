namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class MessageEntityTests
{
    [Fact]
    public void Create_StampsSequenceRequestAndSenderIdentity()
    {
        var requestId = Guid.NewGuid();
        var senderTenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var message = MessageEntity.Create(
            3, 7, requestId, "payload", senderTenantId, membershipId, userId,
            "content", new DateTime(2026, 1, 1), MessageAction.ApplicationAccepted);

        Assert.Equal(3, message.ConversationId);
        Assert.Equal(7, message.Sequence);
        Assert.Equal(requestId, message.RequestId);
        Assert.Equal("payload", message.PayloadHash);
        Assert.Equal(senderTenantId, message.SenderTenantId);
        Assert.Equal(membershipId, message.SentByMembershipId);
        Assert.Equal(userId, message.SentByUserId);
        Assert.Equal(MessageAction.ApplicationAccepted, message.Action);
    }

    [Fact]
    public void Restore_PreservesTheModerationHistory()
    {
        var message = CreateMessage("reported content");
        var hiddenByUserId = Guid.NewGuid();
        var hiddenAt = new DateTime(2026, 8, 15, 12, 0, 0);

        message.Hide(hiddenByUserId, hiddenAt);
        message.Restore(Guid.NewGuid(), hiddenAt.AddHours(1));

        Assert.False(message.IsHidden);
        Assert.Equal(hiddenAt, message.HiddenAt);
        Assert.Equal(hiddenByUserId, message.HiddenByUserId);
        Assert.Equal("reported content", message.Content);
    }

    [Fact]
    public void Hide_AfterRestore_HidesAgain()
    {
        var message = CreateMessage("content");
        var first = new DateTime(2026, 8, 15, 12, 0, 0);

        message.Hide(Guid.NewGuid(), first);
        message.Restore(Guid.NewGuid(), first.AddHours(1));
        message.Hide(Guid.NewGuid(), first.AddHours(2));

        Assert.True(message.IsHidden);
    }

    private static MessageEntity CreateMessage(string content) =>
        MessageEntity.Create(
            3, 1, Guid.NewGuid(), "payload", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            content, new DateTime(2026, 1, 1));
}
