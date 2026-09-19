using Concertable.B2B.Conversations.Domain.ReadModels;

namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class TenantDisplayTests
{
    [Fact]
    public void Apply_UsesOnlyANewerVersion()
    {
        var display = TenantDisplay.Create(Guid.NewGuid(), 3, "Current name");

        display.Apply(2, "Older name");
        display.Apply(3, "Same-version name");
        display.Apply(4, "New name");

        Assert.Equal(4, display.DisplayVersion);
        Assert.Equal("New name", display.DisplayName);
    }
}
