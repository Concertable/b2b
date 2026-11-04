namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class ConversationReadPositionTests
{
    [Fact]
    public void Advance_MovesThePositionForward()
    {
        var position = ConversationReadPosition.Create(3, Guid.NewGuid(), Guid.NewGuid(), 4);

        position.Advance(9);

        Assert.Equal(9, position.LastReadSequence);
    }

    [Fact]
    public void Advance_NeverMovesThePositionBackwards()
    {
        var position = ConversationReadPosition.Create(3, Guid.NewGuid(), Guid.NewGuid(), 9);

        position.Advance(4);

        Assert.Equal(9, position.LastReadSequence);
    }
}
