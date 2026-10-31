namespace Concertable.B2B.Authorization.UnitTests;

public sealed class ResourceAccessTests
{
    public static TheoryData<ResourceAccess, ResourceAccess, bool> Breadths => new()
    {
        { ResourceAccess.All, ResourceAccess.All, true },
        { ResourceAccess.All, ResourceAccess.Assigned, true },
        { ResourceAccess.Assigned, ResourceAccess.Assigned, true },
        { ResourceAccess.Assigned, ResourceAccess.All, false },
        { ResourceAccess.None, ResourceAccess.Assigned, false },
        { ResourceAccess.All, ResourceAccess.None, false },
        { ResourceAccess.None, ResourceAccess.None, false },
    };

    [Theory]
    [MemberData(nameof(Breadths))]
    public void Includes_RequiredAccess_MatchesBreadth(ResourceAccess held, ResourceAccess required, bool expected)
    {
        Assert.Equal(expected, held.Includes(required));
    }

    [Fact]
    public void Values_BroaderAccessComparesGreater()
    {
        Assert.True(ResourceAccess.All > ResourceAccess.Assigned);
        Assert.True(ResourceAccess.Assigned > ResourceAccess.None);
    }

    [Fact]
    public void Values_PersistedNumbersAreStable()
    {
        Assert.Equal(0, (int)ResourceAccess.None);
        Assert.Equal(1, (int)ResourceAccess.Assigned);
        Assert.Equal(2, (int)ResourceAccess.All);
    }
}
