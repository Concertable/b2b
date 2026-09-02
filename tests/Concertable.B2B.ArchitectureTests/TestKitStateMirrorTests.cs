using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.TestKit;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class TestKitStateMirrorTests
{
    [Fact]
    public void B2BConcertLifecycleState_MirrorsConcertState()
    {
        var owned = Enum.GetValues<ConcertState>()
            .Select(state => (state.ToString(), (int)state))
            .ToArray();
        var mirrored = Enum.GetValues<B2BConcertLifecycleState>()
            .Select(state => (state.ToString(), (int)state))
            .ToArray();

        Assert.Equal(owned, mirrored);
    }
}
