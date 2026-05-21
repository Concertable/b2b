namespace Concertable.Organization.UnitTests;

public class OrganizationEntityTests
{
    [Fact]
    public void Create_ReturnsEntity_WithExpectedValues()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var org = OrganizationEntity.Create("Acme Ltd", userId, now);

        Assert.Equal("Acme Ltd", org.LegalName);
        Assert.Equal(userId, org.CreatedByUserId);
        Assert.Equal(now, org.CreatedAt);
    }
}

