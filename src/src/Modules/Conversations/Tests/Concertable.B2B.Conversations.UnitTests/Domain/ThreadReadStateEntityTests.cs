namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class ThreadReadStateEntityTests
{
    [Fact]
    public void Advance_MovesThePointerForward()
    {
        var pointer = ThreadReadStateEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 1, 1));

        pointer.Advance(new DateTime(2026, 2, 1));

        Assert.Equal(new DateTime(2026, 2, 1), pointer.LastReadAt);
    }

    [Fact]
    public void Advance_NeverMovesThePointerBackwards()
    {
        var pointer = ThreadReadStateEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 2, 1));

        pointer.Advance(new DateTime(2026, 1, 1));

        Assert.Equal(new DateTime(2026, 2, 1), pointer.LastReadAt);
    }
}
